using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Phoenix.Common;
using System;
using System.IO;
using System.Web;

namespace Phoenix.Web.Models
{
    public class AssetStorageService : IAssetStorageService
    {
        //public CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["CloudStorageConnection"].ConnectionString);
        public Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));
        
        /// <summary>
        /// .ctor
        /// </summary>
        public AssetStorageService()
        {

        }

        public string SaveFileinStorage(HttpPostedFileBase file, string containerName)
        {
            try
            {
                var blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference(containerName);
                if (container.CreateIfNotExists())
                {
                    // configure container for public access
                    var permissions = container.GetPermissions();
                    permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                    container.SetPermissions(permissions);
                }

                var blobExtn = Path.GetExtension(file.FileName);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(GuidHelper.EncodeTo15() + blobExtn);
                blockBlob.Properties.ContentType = file.ContentType;

                // Upload
                blockBlob.UploadFromStream(file.InputStream);

                return blockBlob.Name;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return "";
        }

        public string SaveFileStreaminStorage(Stream stream, string containerName, string streamType = null,string  fileExtn = null)
        {
            try
            {
                var blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference(containerName);
                if (container.CreateIfNotExists())
                {
                    // configure container for public access
                    var permissions = container.GetPermissions();
                    permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                    container.SetPermissions(permissions);
                }

                var blobExtn = string.IsNullOrWhiteSpace(fileExtn) ? string.Empty : fileExtn;

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(GuidHelper.EncodeTo15() + blobExtn);

                if (string.IsNullOrWhiteSpace(streamType) == false)
                {
                    blockBlob.Properties.ContentType = streamType;
                }

                // Upload
                blockBlob.UploadFromStream(stream);

                return blockBlob.Name;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return "";
        }
        public bool RemoveFromStorage(string blobName, string containerName)
        {
            bool returnVal = false;
            try
            {
                var blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference(containerName);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

                // Delete
                blockBlob.Delete();
                returnVal = true;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return returnVal;
        }
    }

    public interface IAssetStorageService
    {
        string SaveFileinStorage(HttpPostedFileBase file, string containerName);
        bool RemoveFromStorage(string blobName, string containerName);
        string SaveFileStreaminStorage(Stream stream, string containerName, string streamType = null, string fileExtn = null);
    }
}