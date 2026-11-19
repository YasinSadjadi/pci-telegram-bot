// Generated from Messages.resx. Keep properties in sync with resource keys.
using System.Resources;

namespace VocabularyBot.Infrastructure.Resources;

public static class Messages
{
    private static readonly ResourceManager Manager = new("VocabularyBot.Infrastructure.Resources.Messages", typeof(Messages).Assembly);

    private static string Get(string key) => Manager.GetString(key) ?? throw new MissingManifestResourceException(key);

    public static string KeyboardPlaceholder => Get(nameof(KeyboardPlaceholder));
    public static string LanguageGerman => Get(nameof(LanguageGerman));
    public static string LanguageFrench => Get(nameof(LanguageFrench));
    public static string LanguageSpanish => Get(nameof(LanguageSpanish));
    public static string UserRoleAdmin => Get(nameof(UserRoleAdmin));
    public static string UserRoleMember => Get(nameof(UserRoleMember));
    public static string CourseUniversityPrompt => Get(nameof(CourseUniversityPrompt));
    public static string CourseYearPrompt => Get(nameof(CourseYearPrompt));
    public static string CourseTermPrompt => Get(nameof(CourseTermPrompt));
    public static string WordCoursePrompt => Get(nameof(WordCoursePrompt));
    public static string WordEnglishPrompt => Get(nameof(WordEnglishPrompt));
    public static string WordPronunciationPrompt => Get(nameof(WordPronunciationPrompt));
    public static string WordMeaningPrompt => Get(nameof(WordMeaningPrompt));
    public static string WordFamilyPrompt => Get(nameof(WordFamilyPrompt));
    public static string WordSynonymsPrompt => Get(nameof(WordSynonymsPrompt));
    public static string WordTranslationPrompt => Get(nameof(WordTranslationPrompt));
    public static string HelpText => Get(nameof(HelpText));
    public static string AdminHelpText => Get(nameof(AdminHelpText));
    public static string BrowseCoursePrompt => Get(nameof(BrowseCoursePrompt));
    public static string BrowseStudentPrompt => Get(nameof(BrowseStudentPrompt));
    public static string BrowseWordsHeading => Get(nameof(BrowseWordsHeading));
    public static string WelcomeRegistrationPrompt => Get(nameof(WelcomeRegistrationPrompt));
    public static string OperationCancelled => Get(nameof(OperationCancelled));
    public static string StudentNumberPrompt => Get(nameof(StudentNumberPrompt));
    public static string BrowseCourseHint => Get(nameof(BrowseCourseHint));
    public static string AdminIdentifierPrompt => Get(nameof(AdminIdentifierPrompt));
    public static string RetryAdminIdentifier => Get(nameof(RetryAdminIdentifier));
    public static string RetryCourseSelection => Get(nameof(RetryCourseSelection));
    public static string RetryCurrentStep => Get(nameof(RetryCurrentStep));
    public static string WordSavedSummary => Get(nameof(WordSavedSummary));
    public static string RetryWordForm => Get(nameof(RetryWordForm));
    public static string FinishTranslationsPrompt => Get(nameof(FinishTranslationsPrompt));
    public static string TranslationFormatPrompt => Get(nameof(TranslationFormatPrompt));
    public static string TranslationAdded => Get(nameof(TranslationAdded));
    public static string TranslationSaveInstructions => Get(nameof(TranslationSaveInstructions));
    public static string FinishTranslationEntry => Get(nameof(FinishTranslationEntry));
    public static string JoinCoursePrompt => Get(nameof(JoinCoursePrompt));
    public static string HomeHeading => Get(nameof(HomeHeading));
    public static string HomeRegistrationHint => Get(nameof(HomeRegistrationHint));
    public static string HomeMenuHint => Get(nameof(HomeMenuHint));
    public static string FirstWordStep => Get(nameof(FirstWordStep));
    public static string ReenterWordDetails => Get(nameof(ReenterWordDetails));
    public static string FirstStepCancelHint => Get(nameof(FirstStepCancelHint));
    public static string CourseButtonLabel => Get(nameof(CourseButtonLabel));
    public static string PreviousPage => Get(nameof(PreviousPage));
    public static string NextPage => Get(nameof(NextPage));
    public static string AllCourseWords => Get(nameof(AllCourseWords));
    public static string CourseListButton => Get(nameof(CourseListButton));
    public static string StepInputPlaceholder => Get(nameof(StepInputPlaceholder));
    public static string CourseCreated => Get(nameof(CourseCreated));
    public static string AlreadyCourseMember => Get(nameof(AlreadyCourseMember));
    public static string CourseJoined => Get(nameof(CourseJoined));
    public static string EmptyCoursesList => Get(nameof(EmptyCoursesList));
    public static string AllCoursesTitle => Get(nameof(AllCoursesTitle));
    public static string MyCoursesTitle => Get(nameof(MyCoursesTitle));
    public static string CoursesListHeading => Get(nameof(CoursesListHeading));
    public static string CourseListItem => Get(nameof(CourseListItem));
    public static string StudentRegistered => Get(nameof(StudentRegistered));
    public static string LanguageListSeparator => Get(nameof(LanguageListSeparator));
    public static string EmptyWordList => Get(nameof(EmptyWordList));
    public static string MyWordsHeading => Get(nameof(MyWordsHeading));
    public static string PronunciationLabel => Get(nameof(PronunciationLabel));
    public static string PersianMeaningLabel => Get(nameof(PersianMeaningLabel));
    public static string WordFamilyLabel => Get(nameof(WordFamilyLabel));
    public static string SynonymsLabel => Get(nameof(SynonymsLabel));
    public static string AdminAssigned => Get(nameof(AdminAssigned));
    public static string WordRegistered => Get(nameof(WordRegistered));
    public static string WordListSeparator => Get(nameof(WordListSeparator));
    public static string WordHeading => Get(nameof(WordHeading));
}
