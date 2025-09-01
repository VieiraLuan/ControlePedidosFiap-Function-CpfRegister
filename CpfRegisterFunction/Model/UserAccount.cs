namespace CpfRegisterFunction.Model
{
    public class UserAccount
    {
        public bool AccountEnabled { get; set; } = true;
        public string DisplayName { get; set; } = string.Empty;
        public string MailNickname { get; set; } = string.Empty;
        public string UserPrincipalName { get; set; } = string.Empty;
        public PasswordProfile PasswordProfile { get; set; } = new PasswordProfile();
    }
}
