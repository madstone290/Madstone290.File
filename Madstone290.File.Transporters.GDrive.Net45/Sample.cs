using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Madstone290.File.Transporters.GDrive.Net45
{
    internal class Sample
    {
        /// <summary>
        /// 사용법
        /// </summary>
        public void Usage()
        {
            var keyFile = @"madstone-dotnet-gdrive-89595aee92d0.json"; // 클라이언트 인증키 파일
            string[] scopes = new string[] { DriveService.Scope.Drive }; // 스코프

            GoogleDriveTransporter transporter
                = new GoogleDriveTransporter.Builder().AuthenticateServiceAccountJson(keyFile, scopes);

            string filePath = "TextFile1.txt"; // 로컬파일 경로
            string folderId = "1uvMM_BGCcafuHrTFZkt8q0CqcQ69YVBV"; // 파일을 저장할 드라이브 폴더
            string fileName = "TextFile1.txt"; // 드라이브 파일 이름
            string mimeType = "text/plain"; // 드라이브에 적용할 mime 타입.
            string description = "테스트 파일입니다"; // 드라이브 파일 설명

            using (var file = new FileStream(filePath, FileMode.Open))
                transporter.CreateUpload(file, fileName, mimeType, folderId, description);

        }


        /// <summary>
        /// 드라이브에 포함된 모든 파일을 조회한다.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public List<Google.Apis.Drive.v3.Data.File> GetFiles(DriveService service, string search)
        {

            List<Google.Apis.Drive.v3.Data.File> Files = new List<Google.Apis.Drive.v3.Data.File>();

            try
            {
                //List all of the files and directories for the current user.  
                // Documentation: https://developers.google.com/drive/v2/reference/files/list
                FilesResource.ListRequest list = service.Files.List();
                list.Fields = "*";

                list.PageSize = 1000;
                if (search != null)
                {
                    list.Q = search;
                }
                var filesFeed = list.Execute();

                //// Loop through until we arrive at an empty page
                while (filesFeed.Files != null)
                {
                    // Adding each item  to the list.
                    foreach (var item in filesFeed.Files)
                    {
                        Files.Add(item);
                    }

                    // We will know we are on the last page when the next page token is
                    // null.
                    // If this is the case, break.
                    if (filesFeed.NextPageToken == null)
                    {
                        break;
                    }

                    // Prepare the next page of results
                    list.PageToken = filesFeed.NextPageToken;

                    // Execute and process the next page request
                    filesFeed = list.Execute();
                }
            }
            catch (Exception ex)
            {
                // In the event there is an error with the request.
                Console.WriteLine(ex.Message);
            }
            return Files;
        }

        /// <summary>
        /// 드라이브에 폴더를 생성한다.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parent"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public string CreateFolder(DriveService service, string parent, string folderName)
        {
            var driveFolder = new Google.Apis.Drive.v3.Data.File();
            driveFolder.Name = folderName;
            // 마임타입을 폴더로 지정할 것
            driveFolder.MimeType = "application/vnd.google-apps.folder";
            driveFolder.Parents = new string[] { parent };
            var request = service.Files.Create(driveFolder);
            var file = request.Execute();
            return file.Id;
        }



    }
}
