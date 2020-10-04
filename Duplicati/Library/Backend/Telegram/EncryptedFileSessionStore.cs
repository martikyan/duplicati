using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TLSharp.Core;

namespace Duplicati.Library.Backend
{
    public class EncryptedFileSessionStore : ISessionStore
    {
        private readonly string m_password;

        public EncryptedFileSessionStore(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            m_password = password;
        }


        public void Save(Session session)
        {
            var sessionId = session.SessionUserId;
            var filePath = GetSessionFilePath(sessionId);
            var sessionBytes = session.ToBytes();

            WriteToEncryptedStorage(sessionBytes, m_password, filePath);
        }

        public Session Load(string userId)
        {
            var filePath = GetSessionFilePath(userId);
            var sessionBytes = ReadFromEncryptedStorage(m_password, filePath);

            if (sessionBytes == null)
            {
                return null;
            }


            return Session.FromBytes(sessionBytes, this, userId);
        }

        private string GetSessionFilePath(string userId)
        {
            userId = userId.TrimStart('+');
            var sha = GetShortSha(userId);

            var appData = Environment.SpecialFolder.LocalApplicationData;
            var sessionFilePath = Path.Combine(Environment.GetFolderPath(appData), nameof(Duplicati), $"tg_{sha}.dat");

            return sessionFilePath;
        }

        private static void WriteToEncryptedStorage(byte[] bytesToWrite, string pass, string path)
        {
            using (var sessionMs = new MemoryStream(bytesToWrite))
            using (var file = File.Open(path, FileMode.Create, FileAccess.Write))
            {
                SharpAESCrypt.SharpAESCrypt.Encrypt(pass, sessionMs, file);
            }
        }

        private static byte[] ReadFromEncryptedStorage(string pass, string path)
        {
            if (File.Exists(path) == false)
            {
                return null;
            }

            using (var sessionMs = new MemoryStream())
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                SharpAESCrypt.SharpAESCrypt.Decrypt(pass, file, sessionMs);
                return sessionMs.ToArray();
            }
        }

        private static string GetShortSha(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            using (var sha = SHA256.Create())
            {
                var longShaBytes = sha.ComputeHash(inputBytes);
                var longSha = BitConverter.ToString(longShaBytes).Replace("-", string.Empty);
                var result = longSha.Substring(0, 16);

                return result;
            }
        }
    }
}