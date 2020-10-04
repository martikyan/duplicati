using TLSharp.Core;

namespace Duplicati.Library.Backend
{
    public interface IExtendedSessionStore : ISessionStore
    {
        string GetPhoneHash(string phone);

        void SetPhoneHash(string phone, string phoneCodeHash);
    }
}