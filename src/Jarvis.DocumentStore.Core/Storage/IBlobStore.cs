using System.IO;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using System;

namespace Jarvis.DocumentStore.Core.Storage
{
    /// <summary>
    /// This interface identify a storage system capable to store
    /// binary blob of data into a persistence medium.
    /// </summary>
    public interface IBlobStore
    {
        /// <summary>
        /// Get an <see cref="IBlobDescriptor"/> given the <paramref name="blobId"/>
        /// </summary>
        /// <param name="blobId"></param>
        /// <returns></returns>
        IBlobDescriptor GetDescriptor(BlobId blobId);

        /// <summary>
        /// Delete a blob store from the underling storage
        /// </summary>
        /// <param name="blobId"></param>
        void Delete(BlobId blobId);

        /// <summary>
        /// Download a blob into a local file name.
        /// </summary>
        /// <param name="blobId">The id of the blob</param>
        /// <param name="folder">Name of the folder where you want to download the file</param>
        /// <returns>The name of the logically downloaded file.</returns>
        String Download(BlobId blobId, String folder);

        /// <summary>
        /// Create a new file
        /// </summary>
        /// <param name="format"></param>
        /// <param name="fname"></param>
        /// <returns></returns>
        IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname);

        /// <summary>
        /// Upload a file to the underling storage. This version of the function
        /// is based on a local file on disk identified by the parameter <paramref name="pathToFile"/>
        /// </summary>
        /// <param name="format">Format of the blob, depending on format the storage can decide to 
        /// use different type of storage system.</param>
        /// <param name="pathToFile">Local fullpath to file to be uploaded to underling storage system.</param>
        /// <returns>The underling <see cref="BlobId"/> used to identify this blob</returns>
        BlobId Upload(DocumentFormat format, String pathToFile);

        /// <summary>
        /// Upload a file to the underling storage passing a stream as argument. 
        /// </summary>
        /// <param name="format">Format of the blob, depending on format the storage can decide to 
        /// use different type of storage system.</param>
        /// <param name="fileName">Logical name of the file.</param>
        /// <param name="sourceStream">The stream that contains the byte to be written.</param>
        /// <returns>The underling <see cref="BlobId"/> used to identify this blob</returns>
        BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStream);

        /// <summary>
        /// Intdroduced to allow for file reference, this function will store information about
        /// the file specified in <paramref name="pathToFile"/> into the database, but the physical
        /// file will be left on the original path, and not copied inside the real storage of the blob store.
        /// </summary>
        /// <param name="format">Format of the blob, it is needed because we can also store not only 
        /// the original file but also other format, consider if you are referencing files from a system
        /// that already converted file to pdf.</param>
        /// <param name="pathToFile"></param>
        /// <returns></returns>
        BlobId UploadReference(DocumentFormat format, String pathToFile);

        /// <summary>
        /// Blob store is the owner of the file, it stores not only the content but
        /// saves the hash of the file and this can be used to verify if something changed
        /// and the file was tampered with.
        /// </summary>
        /// <remarks>This method does not have <see cref="DocumentFormat"/> because we can check
        /// integrity only of original blob. Checking integrity of artifacts makes no sense.</remarks>
        /// <param name="blobId"></param>
        /// <returns>False if the integrity of the file fails.</returns>
        bool CheckIntegrity(BlobId blobId);

        /// <summary>
        /// Get storage information about this specific store.
        /// </summary>
        /// <returns></returns>
        BlobStoreInfo GetInfo();
    }
}