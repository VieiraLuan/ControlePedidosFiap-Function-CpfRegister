namespace CpfRegisterFunction.Model
{
    public static class UserAccountExtensions
    {
        private const string Domain = "@luandasilvavieirahotmail.onmicrosoft.com";

        public static UserAccount WithUserPrincipalName(this UserAccount account, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty for UserPrincipalName.");

            var normalized = name.Trim().Replace(" ", "").ToLowerInvariant();
            account.UserPrincipalName = normalized + Domain;

            return account;
        }
    }
}
