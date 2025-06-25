namespace CentersBarCode.Services
{
    public class AuthResult
    {
        /// <summary>
        /// Indicates if the authentication was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// The user's email address from Google
        /// </summary>
        public string? UserEmail { get; set; }
        
        /// <summary>
        /// The ID token from Google
        /// </summary>
        public string? IdToken { get; set; }
        
        /// <summary>
        /// The server authentication code that can be exchanged for tokens
        /// </summary>
        public string? ServerAuthCode { get; set; }
        
        /// <summary>
        /// Error message if authentication failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}