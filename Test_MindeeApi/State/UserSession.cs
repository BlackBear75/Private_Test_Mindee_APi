namespace Test_MindeeApi.State
{
    public enum ConversationState
    {
        WaitingForPassport,
        WaitingForPassportFront,
        WaitingForPassportBack,
        WaitingForVehicleDoc,
        WaitingForDataConfirmation,
        WaitingForPriceConfirmation,
        Completed
    }

    public enum ConfirmationStep
    {
        None,
        Passport,
        VehicleDoc
    }

    public class UserSession
    {
        public ConversationState State { get; set; } = ConversationState.WaitingForPassportFront;

        public ConfirmationStep CurrentConfirmationStep { get; set; } = ConfirmationStep.None;

        public string? PassportFrontFileId { get; set; }
        public string? PassportBackFileId { get; set; }
        public string? Passport { get; set; }
        public string? TechPassport { get; set; }
    }
}