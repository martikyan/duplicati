using System;
using System.IO;
using System.Text;
using SharpAESCrypt;
using TLSharp.Core;

namespace Duplicati.Library.Backend
{
    public class EncryptedFileSessionStore : IExtendedSessionStore
    {
        private readonly string m_authCode;

        public EncryptedFileSessionStore(string authCode, Session session)
        {
            if (string.IsNullOrWhiteSpace(authCode))
            {
                throw new ArgumentNullException(nameof(authCode));
            }

            m_authCode = authCode;
            Save(session);
        }


        public void Save(Session session)
        {
            var phoneNumber = session.TLUser.Phone;
            var filePath = GetSessionFilePath(phoneNumber);
            var sessionBytes = session.ToBytes();

            WriteToEncryptedStorage(sessionBytes, filePath);
        }

        public Session Load(string phone)
        {
            var filePath = GetSessionFilePath(phone);
            var sessionBytes = ReadFromEncryptedStorage(filePath);

            return Session.FromBytes(sessionBytes, this, phone);
        }

        private static string GetSessionFilePath(string phoneNumber)
        {
            phoneNumber = phoneNumber.TrimStart('+');

            var appData = Environment.SpecialFolder.LocalApplicationData;
            var sessionFilePath = Path.Combine(Environment.GetFolderPath(appData), $"{phoneNumber}.dat");

            return sessionFilePath;
        }

        private static string GetPhoneHashFilePath(string phoneNumber)
        {
            var sessionFilePath = GetSessionFilePath(phoneNumber);
            sessionFilePath += ".ph";

            return sessionFilePath;
        }

        public string GetPhoneHash(string phone)
        {
            var filePath = GetPhoneHashFilePath(phone);
            var phoneHashBytes = ReadFromEncryptedStorage(filePath);
            if (phoneHashBytes == null)
            {
                return null;
            }

            var phoneHash = Encoding.UTF8.GetString(phoneHashBytes);
            return phoneHash;
        }

        public void SetPhoneHash(string phone, string phoneCodeHash)
        {
            var filePath = GetPhoneHashFilePath(phone);
            var phoneCodeBytes = Encoding.UTF8.GetBytes(phoneCodeHash);

            WriteToEncryptedStorage(phoneCodeBytes, filePath);
        }

        private void WriteToEncryptedStorage(byte[] bytesToWrite, string path)
        {
            using (var file = File.Open(path, FileMode.Create, FileAccess.Write))
            {
                using (var sessionMs = new MemoryStream(bytesToWrite))
                {
                    var cryptoStream = new SharpAESCrypt.SharpAESCrypt(m_authCode, sessionMs, OperationMode.Encrypt);
                    cryptoStream.CopyTo(file);
                }
            }
        }

        private byte[] ReadFromEncryptedStorage(string path)
        {
            if (File.Exists(path) == false)
            {
                return null;
            }

            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                using (var sessionMs = new MemoryStream())
                {
                    var cryptoStream = new SharpAESCrypt.SharpAESCrypt(m_authCode, file, OperationMode.Decrypt);
                    cryptoStream.CopyTo(sessionMs);
                    return sessionMs.ToArray();
                }
            }
        }
    }
}