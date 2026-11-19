// Generated from Errors.resx. Keep properties in sync with resource keys.
using System.Resources;

namespace VocabularyBot.Infrastructure.Resources;

public static class Error
{
    private static readonly ResourceManager Manager = new("VocabularyBot.Infrastructure.Resources.Errors", typeof(Error).Assembly);

    private static string Get(string key) => Manager.GetString(key) ?? throw new MissingManifestResourceException(key);

    public static string ExpiredBrowseSelection => Get(nameof(ExpiredBrowseSelection));
    public static string InvalidBrowseCourse => Get(nameof(InvalidBrowseCourse));
    public static string CourseStudentNotFound => Get(nameof(CourseStudentNotFound));
    public static string SelectedWordsNotFound => Get(nameof(SelectedWordsNotFound));
    public static string InvalidBrowseSelection => Get(nameof(InvalidBrowseSelection));
    public static string ExpiredCourseSelection => Get(nameof(ExpiredCourseSelection));
    public static string StartRequired => Get(nameof(StartRequired));
    public static string StudentRegistrationRequired => Get(nameof(StudentRegistrationRequired));
    public static string StudentRegistrationPromptRequired => Get(nameof(StudentRegistrationPromptRequired));
    public static string CourseMembershipRequired => Get(nameof(CourseMembershipRequired));
    public static string UnknownCommand => Get(nameof(UnknownCommand));
    public static string RequiredFieldInvalid => Get(nameof(RequiredFieldInvalid));
    public static string PositiveNumberRequired => Get(nameof(PositiveNumberRequired));
    public static string UniversityNameTooLong => Get(nameof(UniversityNameTooLong));
    public static string UnsupportedOperation => Get(nameof(UnsupportedOperation));
    public static string TranslationLengthInvalid => Get(nameof(TranslationLengthInvalid));
    public static string WordFormIncomplete => Get(nameof(WordFormIncomplete));
    public static string RequiredFieldMissing => Get(nameof(RequiredFieldMissing));
    public static string InputLengthInvalid => Get(nameof(InputLengthInvalid));
    public static string InvalidCourseMembership => Get(nameof(InvalidCourseMembership));
    public static string NoCoursesAvailable => Get(nameof(NoCoursesAvailable));
    public static string InvalidCourseTerm => Get(nameof(InvalidCourseTerm));
    public static string CourseNotFound => Get(nameof(CourseNotFound));
    public static string AccessDenied => Get(nameof(AccessDenied));
    public static string InvalidStudentNumber => Get(nameof(InvalidStudentNumber));
    public static string RegisteredUserNotFound => Get(nameof(RegisteredUserNotFound));
    public static string InvalidAdminRequest => Get(nameof(InvalidAdminRequest));
    public static string RequiredWordDetailsMissing => Get(nameof(RequiredWordDetailsMissing));
    public static string InvalidWordCourseMembership => Get(nameof(InvalidWordCourseMembership));
    public static string InvalidTranslationFormat => Get(nameof(InvalidTranslationFormat));
    public static string DuplicateTranslationLanguage => Get(nameof(DuplicateTranslationLanguage));
    public static string AdminCommandRequired => Get(nameof(AdminCommandRequired));
    public static string AdminAccessRequired => Get(nameof(AdminAccessRequired));
    public static string InvalidSupportedTranslation => Get(nameof(InvalidSupportedTranslation));
    public static string DuplicateWordRetry => Get(nameof(DuplicateWordRetry));
    public static string MessageProcessingFailed => Get(nameof(MessageProcessingFailed));
    public static string CourseAdminRequired => Get(nameof(CourseAdminRequired));
    public static string InvalidUniversityName => Get(nameof(InvalidUniversityName));
    public static string DuplicateStudentNumber => Get(nameof(DuplicateStudentNumber));
    public static string PositiveNumberPrompt => Get(nameof(PositiveNumberPrompt));
    public static string ValidIdentifierPrompt => Get(nameof(ValidIdentifierPrompt));
    public static string ValidDatePrompt => Get(nameof(ValidDatePrompt));
    public static string DuplicateWord => Get(nameof(DuplicateWord));
    public static string OperationFailed => Get(nameof(OperationFailed));
}
