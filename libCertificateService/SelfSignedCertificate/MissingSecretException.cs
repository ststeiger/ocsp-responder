
namespace SimpleChallengeResponder
{


    public class MissingSecretException 
        : System.Exception
    {
        public string SecretName { get; }

        public MissingSecretException(
            string secretName
        )
            : base($"Missing secret: '{secretName}'.")
        {
            this.SecretName = secretName;
        }

        public MissingSecretException(
            string secretName, 
            System.Exception innerException
        )
            : base($"Missing secret: '{secretName}'.", innerException)
        {
            this.SecretName = secretName;
        }
    }


}
