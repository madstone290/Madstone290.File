using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GFile = Google.Apis.Drive.v3.Data.File;

namespace Madstone290.File.Transporters.GDrive.Net45
{
    /// <summary>
    /// 구글 드라이브를 사용하여 파일을 전송한다.
    /// </summary>
    public class GoogleDriveTransporter
    {
        /// <summary>
        /// 전송자를 생성한다.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// OAuth 인증을 사용한다.
            /// </summary>
            /// <param name="clientSecretPath">클라이언트 인증파일 경로</param>
            /// <param name="userId">드라이브 사용자ID</param>
            /// <param name="scopes">드라이브 스코프</param>
            /// <param name="storePath">사용자 토큰저장소 경로</param>
            /// <returns></returns>
            public GoogleDriveTransporter AuthenticateOAuth(string clientSecretPath, string userId, string[] scopes, string storePath)
            {
                UserCredential credential;

                using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
                {
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    var clientSecret = GoogleClientSecrets.FromStream(stream).Secrets;

                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        clientSecret,
                        scopes,
                        userId,
                        CancellationToken.None,
                        new FileDataStore(storePath, true)).Result;
                }

                // Create Drive API service.
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                });

                return new GoogleDriveTransporter(service);
            }


            /// <summary>
            /// Json파일을 이용하여 service account 인증을 진행한다.
            /// Documentation: https://developers.google.com/accounts/docs/OAuth2#serviceaccount
            /// </summary>
            /// <param name="serviceAccountCredentialFilePath">Location of the .p12 or Json Service account key file downloaded from Google Developer console https://console.developers.google.com</param>
            /// <param name="scopes"></param>
            /// <returns>AnalyticsService used to make requests against the Analytics API</returns>
            public GoogleDriveTransporter AuthenticateServiceAccountJson(string serviceAccountCredentialFilePath, string[] scopes)
            {
                if (string.IsNullOrEmpty(serviceAccountCredentialFilePath))
                    throw new Exception("Path to the service account credentials file is required.");
                if (!System.IO.File.Exists(serviceAccountCredentialFilePath))
                    throw new Exception("The service account credentials file does not exist at: " + serviceAccountCredentialFilePath);
                if (Path.GetExtension(serviceAccountCredentialFilePath).ToLower() != ".json")
                    throw new Exception("Service account credential is not json file.");

                try
                {
                    GoogleCredential credential;
                    using (var stream = new FileStream(serviceAccountCredentialFilePath, FileMode.Open, FileAccess.Read))
                    {
                        credential = GoogleCredential.FromStream(stream)
                                .CreateScoped(scopes);
                    }

                    DriveService service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential
                    });

                    return new GoogleDriveTransporter(service);
                }
                catch (Exception ex)
                {
                    throw new Exception("CreateServiceAccountDriveFailed", ex);
                }
            }


            /// <summary>
            /// P12파일을 이용하여 service account 인증을 진행한다.
            /// Documentation: https://developers.google.com/accounts/docs/OAuth2#serviceaccount
            /// </summary>
            /// <param name="serviceAccountEmail"></param>
            /// <param name="serviceAccountCredentialFilePath"></param>
            /// <param name="scopes"></param>
            /// <returns></returns>
            public GoogleDriveTransporter AuthenticateServiceAccountP12(string serviceAccountCredentialFilePath, string serviceAccountEmail, string[] scopes)
            {
                if (string.IsNullOrEmpty(serviceAccountCredentialFilePath))
                    throw new Exception("Path to the service account credentials file is required.");
                if (!System.IO.File.Exists(serviceAccountCredentialFilePath))
                    throw new Exception("The service account credentials file does not exist at: " + serviceAccountCredentialFilePath);
                if (Path.GetExtension(serviceAccountCredentialFilePath).ToLower() != ".p12")
                    throw new Exception("Service account credential is not p12 file.");
                if (string.IsNullOrEmpty(serviceAccountEmail))
                    throw new Exception("ServiceAccountEmail is required.");

                var certificate = new X509Certificate2(serviceAccountCredentialFilePath, "notasecret", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(serviceAccountEmail)
                {
                    Scopes = scopes
                }.FromCertificate(certificate));

                DriveService service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential
                });

                return new GoogleDriveTransporter(service);
            }

        }


        private readonly DriveService drivesSerive;

        public DriveService DriveService => drivesSerive;

        private GoogleDriveTransporter(DriveService drivesSerive)
        {
            this.drivesSerive = drivesSerive;
        }

        /// <summary>
        /// Creates a copy of a file and applies any requested updates with patch semantics. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/copy
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="service">Authenticated Drive service.</param>  
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="body">A valid Drive v3 body.</param>
        /// <param name="optional">Optional paramaters.</param>
        /// <returns>FileResponse</returns>
        public GFile Copy(string fileId, GFile body)
        {
            try
            {
                // Initial validation.
                if (body == null)
                    throw new ArgumentNullException("body");
                if (fileId == null)
                    throw new ArgumentNullException("fileId");

                // Building the initial request.
                var request = drivesSerive.Files.Copy(body, fileId);

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Copy failed.", ex);
            }
        }

        /// <summary>
        /// Creates a new file. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/create
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="body">A valid Drive v3 body.</param>
        /// <returns>FileResponse</returns>
        public GFile Create(GFile body)
        {
            try
            {
                // Initial validation.
                if (body == null)
                    throw new ArgumentNullException("body");

                // Building the initial request.
                var request = drivesSerive.Files.Create(body);

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Create failed.", ex);
            }
        }

        /// <summary>
        /// 컨텐츠가 포함된 파일을 생성한다.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileName"></param>
        /// <param name="fileMime"></param>
        /// <param name="folder"></param>
        /// <param name="fileDescription"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string CreateUpload(Stream file, string fileName, string fileMime, string folder, string fileDescription)
        {
            try
            {
                var driveFile = new GFile();
                driveFile.Name = fileName;
                driveFile.Description = fileDescription;
                driveFile.MimeType = fileMime;
                driveFile.Parents = new string[] { folder };

                var request = drivesSerive.Files.Create(driveFile, file, fileMime);
                request.Fields = "id"; // 모든 필드가 필요한 경우 "*" 사용


                var response = request.Upload();
                if (response.Status != Google.Apis.Upload.UploadStatus.Completed)
                    throw response.Exception;

                return request.ResponseBody.Id;
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Create failed.", ex);
            }
        }

        /// <summary>
        /// 컨텐츠가 포함된 파일을 업데이트한다.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="fileId"></param>
        /// <param name="file"></param>
        /// <param name="fileMime"></param>
        /// <returns></returns>
        /// <exception cref="Exception">파일 업데이트 실패</exception>
        public string UpdateUpload(GFile body, string fileId, Stream file, string fileMime)
        {
            try
            {
                var request = drivesSerive.Files.Update(body, fileId, file, fileMime);
                request.Fields = "id";

                var response = request.Upload();
                if (response.Status != Google.Apis.Upload.UploadStatus.Completed)
                    throw response.Exception;

                return request.ResponseBody.Id;
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Create failed.", ex);
            }
        }


        /// <summary>
        /// Permanently deletes a file owned by the user without moving it to the trash. If the file belongs to a Team Drive the user must be an organizer on the parent. If the target is a folder, all descendants owned by the user are also deleted. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/delete
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="fileId">The ID of the file.</param>
        public void Delete(string fileId)
        {
            try
            {
                // Initial validation.
                if (fileId == null)
                    throw new ArgumentNullException("fileId");

                // Building the initial request.
                var request = drivesSerive.Files.Delete(fileId);

                // Requesting data.
                request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Delete failed.", ex);
            }
        }

        /// <summary>
        /// Permanently deletes all of the user's trashed files. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/emptyTrash
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        public void EmptyTrash()
        {
            try
            {
                // Make the request.
                drivesSerive.Files.EmptyTrash().Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.EmptyTrash failed.", ex);
            }
        }

        /// <summary>
        /// Exports a Google Doc to the requested MIME type and returns the exported content. Please note that the exported content is limited to 10MB. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/export
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="mimeType">The MIME type of the format requested for this export.</param>
        public void Export(string fileId, string mimeType)
        {
            try
            {
                // Initial validation.
                if (drivesSerive == null)
                    throw new ArgumentNullException("service");
                if (fileId == null)
                    throw new ArgumentNullException("fileId");
                if (mimeType == null)
                    throw new ArgumentNullException("mimeType");

                // Make the request.
                drivesSerive.Files.Export(fileId, mimeType).Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Export failed.", ex);
            }
        }

        /// <summary>
        /// Generates a set of file IDs which can be provided in create requests. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/generateIds
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <returns>GeneratedIdsResponse</returns>
        public GeneratedIds GenerateIds()
        {
            try
            {
                // Building the initial request.
                var request = drivesSerive.Files.GenerateIds();

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.GenerateIds failed.", ex);
            }
        }

        /// <summary>
        /// Gets a file's metadata or content by ID. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/get
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="service">Authenticated Drive service.</param>  
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="optional">Optional paramaters.</param>
        /// <returns>FileResponse</returns>
        public GFile Get(string fileId)
        {
            try
            {
                if (fileId == null)
                    throw new ArgumentNullException("fileId");

                // Building the initial request.
                var request = drivesSerive.Files.Get(fileId);

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Get failed.", ex);
            }
        }

        /// <summary>
        /// Lists or searches files. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/list
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <returns>FileListResponse</returns>
        public FileList List()
        {
            try
            {
                // Building the initial request.
                var request = drivesSerive.Files.List();

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.List failed.", ex);
            }
        }

        /// <summary>
        /// Updates a file's metadata and/or content with patch semantics. 
        /// Documentation https://developers.google.com/drive/v3/reference/files/update
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="body">A valid Drive v3 body.</param>
        /// <returns>FileResponse</returns>
        public GFile Update(string fileId, GFile body)
        {
            try
            {
                if (body == null)
                    throw new ArgumentNullException("body");
                if (fileId == null)
                    throw new ArgumentNullException("fileId");

                // Building the initial request.
                var request = drivesSerive.Files.Update(body, fileId);

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Update failed.", ex);
            }
        }

        /// <summary>
        /// Subscribes to changes to a file 
        /// Documentation https://developers.google.com/drive/v3/reference/files/watch
        /// Generation Note: This does not always build corectly.  Google needs to standardise things I need to figuer out which ones are wrong.
        /// </summary>
        /// <param name="fileId">The ID of the file.</param>
        /// <param name="body">A valid Drive v3 body.</param>
        /// <returns>ChannelResponse</returns>
        public Channel Watch(string fileId, Channel body)
        {
            try
            {
                // Initial validation.
                if (body == null)
                    throw new ArgumentNullException("body");
                if (fileId == null)
                    throw new ArgumentNullException("fileId");

                // Building the initial request.
                var request = drivesSerive.Files.Watch(body, fileId);

                // Requesting data.
                return request.Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Watch failed.", ex);
            }
        }



    }
}
