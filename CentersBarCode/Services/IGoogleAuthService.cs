using System.Threading.Tasks;

namespace CentersBarCode.Services
{
    public interface IGoogleAuthService
    {
        /// <summary>
        /// Initiates the Google Sign-In flow and returns the authentication result
        /// </summary>
        /// <returns>Authentication result containing tokens and user information</returns>
        Task<AuthResult> SignInWithGoogleAsync();
        
        /// <summary>
        /// Signs the user out of Google
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SignOutAsync();
    }
}