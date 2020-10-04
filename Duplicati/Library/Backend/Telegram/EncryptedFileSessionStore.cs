using System;
using System.IO;
using TLSharp.Core;

namespace Duplicati.Library.Backend
{
    public class EncryptedFileSessionStore : ISessionStore
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

            using (var file = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                using (var sessionMs = new MemoryStream(sessionBytes))
                {
                    var cryptoStream = new SharpAESCrypt.SharpAESCrypt(m_authCode, sessionMs, SharpAESCrypt.OperationMode.Encrypt);
                    cryptoStream.CopyTo(file);
                }
            }
        }

        public Session Load(string phone)
        {
            var filePath = GetSessionFilePath(phone);
            using (var file = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var sessionMs = new MemoryStream())
                {
                    var cryptoStream = new SharpAESCrypt.SharpAESCrypt(m_authCode, file, SharpAESCrypt.OperationMode.Decrypt);
                    cryptoStream.CopyTo(sessionMs);

                    var session = Session.FromBytes(sessionMs.ToArray(), this, phone);
                    return session;
                }
            }
        }
        
        private static string GetSessionFilePath(string phoneNumber)
        {
            phoneNumber = phoneNumber.TrimStart('+');

            var appData = Environment.SpecialFolder.LocalApplicationData;
            var sessionFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), phoneNumber);

            return sessionFilePath;
        }
    }
}