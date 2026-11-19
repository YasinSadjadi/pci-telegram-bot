using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VocabularyBot.Application.Courses;
using VocabularyBot.Application.Students.RegisterStudent;
using VocabularyBot.Application.Words;
using VocabularyBot.Application.Words.ListMyWords;
using VocabularyBot.Application.Workflows.AssignAdmin;
using VocabularyBot.Application.Workflows.RegisterWord;
using VocabularyBot.Domain;
using VocabularyBot.Domain.Words;
using VocabularyBot.Infrastructure.Persistence.Repositories;
using Messages = VocabularyBot.Infrastructure.Resources.Messages;
using Error = VocabularyBot.Infrastructure.Resources.Error;

namespace VocabularyBot.Application.Conversations;

public sealed class BotService(IGenericRepository repository, IMediator mediator, BotOptions options)
{
    private static string[] CoursePrompts =>
    [
        global::VocabularyBot.Infrastructure.Resources.Messages.CourseUniversityPrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.CourseYearPrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.CourseTermPrompt
    ];

    private static string[] WordPrompts =>
    [
        global::VocabularyBot.Infrastructure.Resources.Messages.WordCoursePrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.WordEnglishPrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.WordPronunciationPrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.WordMeaningPrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.WordFamilyPrompt,
        global::VocabularyBot.Infrastructure.Resources.Messages.WordSynonymsPrompt,
        string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.WordTranslationPrompt, TranslationInput.SupportedLanguages)
    ];

    private static string Help(bool admin)
    {
        return
            global::VocabularyBot.Infrastructure.Resources.Messages.HelpText +
            (admin
                ? global::VocabularyBot.Infrastructure.Resources.Messages.AdminHelpText
                : "");
    }

    public async Task<string> Handle(long id, string? username, string name, string input)
    {
        input = BotMenu.Command(input.Trim());
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == id);
        if (input == "/start")
        {
            if (user is null)
            {
                user = new User { TelegramId = id };
                repository.Create(user);
            }

            user.Name = name;
            user.Username = username;
            if (!string.IsNullOrEmpty(username))
            {
                var normalizedUsername = username.ToLowerInvariant();
                var pending = await repository.Query<PendingAdmin>()
                    .SingleOrDefaultAsync(x => x.Username == normalizedUsername);
                if (pending is not null)
                {
                    user.IsAdmin = true;
                    repository.Delete(pending);
                }
            }

            if (id == options.BootstrapAdminId ||
                (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(options.BootstrapAdminUsername) &&
                 string.Equals(username.TrimStart('@'), options.BootstrapAdminUsername.Trim().TrimStart('@'),
                     StringComparison.OrdinalIgnoreCase) &&
                 !await repository.Query<User>().AnyAsync(x => x.IsAdmin)))
                user.IsAdmin = true;
            await repository.SaveChangesAsync();
            if (!HasProfile(user))
            {
                var registration = await RegistrationState(user);
                return RegistrationPrompt(registration);
            }

            return await AfterRegistration(user);
        }

        if (user is not null && !HasProfile(user))
        {
            var registration = await RegistrationState(user);
            return await Register(user, registration, input);
        }

        if (input.StartsWith("browse:"))
        {
            var browseState = user is null
                ? null
                : await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            var parts = input.Split(':');
            if (user is null || browseState?.Flow != "browse" || parts.Length != 4 ||
                parts[1] != SelectionKey(browseState)) return global::VocabularyBot.Infrastructure.Resources.Error.ExpiredBrowseSelection;
            if (parts[2] == "page")
                return browseState.Step == 0
                    ? global::VocabularyBot.Infrastructure.Resources.Messages.BrowseCoursePrompt
                    : global::VocabularyBot.Infrastructure.Resources.Messages.BrowseStudentPrompt;
            if (parts[2] == "classes")
            {
                browseState.Step = 0;
                browseState.Data = "[]";
                await repository.SaveChangesAsync();
                return global::VocabularyBot.Infrastructure.Resources.Messages.BrowseCoursePrompt;
            }

            if (parts[2] == "course" && browseState.Step == 0 && Guid.TryParse(parts[3], out var courseId) &&
                await CanBrowseCourse(user, courseId))
            {
                browseState.Step = 1;
                browseState.Data = JsonSerializer.Serialize(new[] { courseId.ToString() });
                await repository.SaveChangesAsync();
                return global::VocabularyBot.Infrastructure.Resources.Messages.BrowseStudentPrompt;
            }

            if (browseState.Step == 1 && (parts[2] == "student" || parts[2] == "all"))
            {
                var data = JsonSerializer.Deserialize<List<string>>(browseState.Data) ?? [];
                if (data.Count != 1 || !Guid.TryParse(data[0], out var selectedId) ||
                    !await CanBrowseCourse(user, selectedId)) return global::VocabularyBot.Infrastructure.Resources.Error.InvalidBrowseCourse;
                Guid studentId = default;
                if (parts[2] == "student" && (!Guid.TryParse(parts[3], out studentId) || !await repository
                        .Query<CourseMembership>()
                        .AnyAsync(m => m.CourseId == selectedId && m.UserId == studentId)))
                    return global::VocabularyBot.Infrastructure.Resources.Error.CourseStudentNotFound;
                var query = repository.Query<VocabularyEntry>(false).Where(w => w.CourseId == selectedId);
                User? student = null;
                if (parts[2] == "student")
                {
                    student = await repository.Query<User>(false).SingleAsync(u => u.Id == studentId);
                    query = query.Where(w => w.UserId == studentId);
                }
                var words = await query.OrderBy(w => w.EnglishWord).ToListAsync();
                var heading = string.Format(Messages.BrowseWordsHeading, words.Count);
                if (student is not null)
                {
                    var total = await repository.Query<VocabularyEntry>(false).CountAsync(w => w.UserId == studentId);
                    heading = string.Format(Messages.StudentWordStats, StudentLabel(student), total, words.Count);
                }
                if (words.Count == 0) return heading + Error.SelectedWordsNotFound;
                var ids = words.Select(w => w.Id).ToArray();
                var translations = await repository.Query<WordTranslation>(false)
                    .Where(t => ids.Contains(t.VocabularyEntryId)).OrderBy(t => t.Type).ToListAsync();
                var lookup = translations.ToLookup(t => t.VocabularyEntryId);
                return heading + string.Join(global::VocabularyBot.Infrastructure.Resources.Messages.WordListSeparator,
                    words.Select(w => ListMyWordsHandler.Format(w, lookup[w.Id])));
            }

            return global::VocabularyBot.Infrastructure.Resources.Error.InvalidBrowseSelection;
        }

        if (input.StartsWith("class:") || input.StartsWith("page:"))
        {
            var current = user is null
                ? null
                : await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            var parts = input.Split(':');
            if (current is null || parts.Length != 3 || parts[1] != SelectionKey(current) ||
                !(current.Flow == "select_class" || (current.Flow == "word" && current.Step == 0)))
                return global::VocabularyBot.Infrastructure.Resources.Error.ExpiredCourseSelection;
            if (parts[0] == "page") return current.Flow == "word" ? WordPrompts[0] : await ClassSelectionPrompt();
            input = parts[2] == "cancel" ? "/cancel" : parts[2];
        }

        if (user is null) return global::VocabularyBot.Infrastructure.Resources.Error.StartRequired;
        if (input == "/cancel")
        {
            await ClearFlow(user.Id);
            return global::VocabularyBot.Infrastructure.Resources.Messages.OperationCancelled + Home();
        }

        if (input == "/menu")
        {
            await ClearFlow(user.Id);
            return Home();
        }

        if (input == "/help") return Help(user.IsAdmin);
        if (input == "/back") return await Back(user);
        if (input == "/register")
        {
            await StartFlow(user.Id, "register");
            return Messages.StudentNumberPrompt;
        }

        if (input == "/classes")
        {
            var description = await mediator.Send(new ListCoursesQuery(id));
            await StartFlow(user.Id, "browse");
            return description + global::VocabularyBot.Infrastructure.Resources.Messages.BrowseCourseHint;
        }

        if (input == "/join")
        {
            await StartFlow(user.Id, "select_class");
            return await ClassSelectionPrompt();
        }

        if (input == "/browse")
        {
            if (!user.IsAdmin) return global::VocabularyBot.Infrastructure.Resources.Error.AdminCommandRequired;
            await StartFlow(user.Id, "browse");
            return global::VocabularyBot.Infrastructure.Resources.Messages.BrowseCoursePrompt;
        }

        if (input == "/admin")
        {
            if (!user.IsAdmin) return global::VocabularyBot.Infrastructure.Resources.Error.AdminAccessRequired;
            await StartFlow(user.Id, "admin");
            return
                global::VocabularyBot.Infrastructure.Resources.Messages.AdminIdentifierPrompt;
        }

        if (input == "/course")
        {
            if (!user.IsAdmin) return global::VocabularyBot.Infrastructure.Resources.Error.AdminAccessRequired;
            await StartFlow(user.Id, "course");
            return CoursePrompts[0];
        }

        if (input == "/words") return await mediator.Send(new ListMyWordsQuery(id));
        if (input == "/word")
        {
            var memberships = await repository.Query<CourseMembership>(false).Where(m => m.UserId == user.Id)
                .Select(m => m.CourseId).ToListAsync();
            if (memberships.Count == 0) return global::VocabularyBot.Infrastructure.Resources.Error.CourseMembershipRequired;
            await StartFlow(user.Id, "word");
            if (memberships.Count == 1)
            {
                var wordState = await repository.Query<Conversation>().SingleAsync(c => c.UserId == user.Id);
                wordState.Data = JsonSerializer.Serialize(new[] { memberships[0].ToString() });
                wordState.Step = 1;
                await repository.SaveChangesAsync();
                return WordPrompts[1];
            }

            return WordPrompts[0];
        }

        if (input is not ("/skip" or "/done") && Regex.IsMatch(input, @"^/[A-Za-z_]+$"))
            return global::VocabularyBot.Infrastructure.Resources.Error.UnknownCommand;
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state is null) return Home();
        if (state.Flow == "register") return await Register(user, state, input);

        if (state.Flow == "admin")
        {
            if (!user.IsAdmin)
            {
                await ClearFlow(user.Id);
                return global::VocabularyBot.Infrastructure.Resources.Error.AdminCommandRequired;
            }

            var result = await mediator.Send(new AssignAdminCommand(id, [input]));
            if (result.IsSuccessful)
            {
                await ClearFlow(user.Id);
                return result.Message;
            }

            return result.Message + global::VocabularyBot.Infrastructure.Resources.Messages.RetryAdminIdentifier;
        }

        if (state.Flow == "select_class")
        {
            var result = await mediator.Send(new JoinCourseCommand(id, input));
            if (result.IsSuccessful)
            {
                await ClearFlow(user.Id);
                return result.Message;
            }

            return result.Message + global::VocabularyBot.Infrastructure.Resources.Messages.RetryCourseSelection;
        }

        if (state.Flow == "course")
        {
            if (!user.IsAdmin)
            {
                await ClearFlow(user.Id);
                return global::VocabularyBot.Infrastructure.Resources.Error.AdminCommandRequired;
            }

            var fields = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
            if (string.IsNullOrWhiteSpace(input) || input.Length > 2000 || input is "/skip" or "/done")
                return global::VocabularyBot.Infrastructure.Resources.Error.RequiredFieldInvalid;
            if (fields.Count > 0)
            {
                var digits = Digits.ToAscii(input);
                if (!int.TryParse(digits, out var value) || value <= 0)
                    return global::VocabularyBot.Infrastructure.Resources.Error.PositiveNumberRequired;
            }

            if (fields.Count == 0 && input.Length > 200)
                return global::VocabularyBot.Infrastructure.Resources.Error.UniversityNameTooLong;
            fields.Add(input);
            if (fields.Count < CoursePrompts.Length)
            {
                state.Data = JsonSerializer.Serialize(fields);
                state.Step = fields.Count;
                await repository.SaveChangesAsync();
                return CoursePrompts[state.Step];
            }

            var result = await mediator.Send(new CreateCourseCommand(id, fields[0], fields[1], fields[2]));
            if (result.IsSuccessful)
            {
                await ClearFlow(user.Id);
                return result.Message;
            }

            return result.Message + global::VocabularyBot.Infrastructure.Resources.Messages.RetryCurrentStep;
        }

        if (state.Flow != "word")
        {
            await ClearFlow(user.Id);
            return global::VocabularyBot.Infrastructure.Resources.Error.UnsupportedOperation;
        }

        var values = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
        if (values.Count >= 6)
        {
            if (input == "/done")
            {
                var completed = await mediator.Send(new RegisterWordCommand(id, values));
                if (completed.IsSuccessful)
                {
                    await ClearFlow(user.Id);
                    return
                        string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.WordSavedSummary, values[1], values[3]);
                }

                return completed.Message + global::VocabularyBot.Infrastructure.Resources.Messages.RetryWordForm;
            }

            if (input == "/skip") return global::VocabularyBot.Infrastructure.Resources.Messages.FinishTranslationsPrompt;
            if (input.Length == 0 || input.Length > 2000) return global::VocabularyBot.Infrastructure.Resources.Error.TranslationLengthInvalid;
            var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0) return global::VocabularyBot.Infrastructure.Resources.Messages.TranslationFormatPrompt;
            var parsed = new List<(LanguageType Type, string Meaning)>();
            foreach (var line in lines)
            {
                if (!TranslationInput.TryParse(line, out var type, out var meaning))
                    return
                        string.Format(global::VocabularyBot.Infrastructure.Resources.Error.InvalidSupportedTranslation, TranslationInput.SupportedLanguages);
                parsed.Add((type, meaning));
            }

            foreach (var item in parsed)
            {
                var index = values.FindIndex(6,
                    line => TranslationInput.TryParse(line, out var previousType, out _) && previousType == item.Type);
                var line = $"{TranslationInput.Name(item.Type)}: {item.Meaning}";
                if (index < 0) values.Add(line);
                else values[index] = line;
            }

            state.Data = JsonSerializer.Serialize(values);
            await repository.SaveChangesAsync();
            return global::VocabularyBot.Infrastructure.Resources.Messages.TranslationAdded + string.Join("\n", values.Skip(6)) +
                   global::VocabularyBot.Infrastructure.Resources.Messages.TranslationSaveInstructions;
        }

        if (input == "/done") return global::VocabularyBot.Infrastructure.Resources.Error.WordFormIncomplete;
        if (input == "/skip" && values.Count < 4) return global::VocabularyBot.Infrastructure.Resources.Error.RequiredFieldMissing;
        if (input.Length == 0 || input.Length > 2000)
            return global::VocabularyBot.Infrastructure.Resources.Error.InputLengthInvalid;
        if (values.Count == 0)
            if (!Guid.TryParse(input, out var courseId) || !await repository.Query<CourseMembership>()
                    .AnyAsync(m => m.CourseId == courseId && m.UserId == user.Id))
                return global::VocabularyBot.Infrastructure.Resources.Error.InvalidCourseMembership;
        if (values.Count == 1 && input != "/skip")
        {
            var normalized = WordNormalizer.Normalize(input);
            if (await repository.Query<VocabularyEntry>().AnyAsync(x => x.NormalizedWord == normalized))
                return global::VocabularyBot.Infrastructure.Resources.Error.DuplicateWordRetry;
        }

        values.Add(input == "/skip" ? "" : input);
        if (values.Count < WordPrompts.Length)
        {
            state.Data = JsonSerializer.Serialize(values);
            state.Step = values.Count;
            await repository.SaveChangesAsync();
            return WordPrompts[state.Step];
        }

        return global::VocabularyBot.Infrastructure.Resources.Messages.FinishTranslationEntry;
    }

    private async Task<bool> CanBrowseCourse(User user, Guid courseId)
    {
        return await repository.Query<Course>(false).AnyAsync(c => c.Id == courseId) &&
               (user.IsAdmin || await repository.Query<CourseMembership>(false)
                   .AnyAsync(m => m.CourseId == courseId && m.UserId == user.Id));
    }

    private async Task<string> ClassSelectionPrompt()
    {
        if (!await repository.Query<Course>(false).AnyAsync())
            return global::VocabularyBot.Infrastructure.Resources.Error.NoCoursesAvailable;
        return
            global::VocabularyBot.Infrastructure.Resources.Messages.JoinCoursePrompt;
    }

    private static string StudentLabel(User student) =>
        $"{student.FullName ?? Messages.StudentNameMissing} ({student.StudentNumber})";

    private static bool HasProfile(User user) =>
        user.StudentNumber is not null && !string.IsNullOrWhiteSpace(user.FullName);

    private async Task<Conversation> RegistrationState(User user)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        var firstStep = user.StudentNumber is null ? 0 : 1;
        if (state?.Flow == "register" && state.Step >= firstStep) return state;
        return await StartFlow(user.Id, "register", firstStep);
    }

    private static string RegistrationPrompt(Conversation state) =>
        state.Step == 0 ? Messages.StudentNumberPrompt : Messages.FullNamePrompt;

    private async Task<string> Register(User user, Conversation state, string input)
    {
        if (input == "/back" && state.Step == 1 && state.Data != "[]")
        {
            state.Step = 0;
            state.Data = "[]";
            await repository.SaveChangesAsync();
            return RegistrationPrompt(state);
        }

        if (input.StartsWith('/') || input.StartsWith("browse:") ||
            input.StartsWith("class:") || input.StartsWith("page:"))
            return RegistrationPrompt(state);

        if (state.Step == 0)
        {
            var number = RegisterStudentHandler.NormalizeNumber(input);
            if (number is null) return Error.InvalidStudentNumber;
            if (await repository.Query<User>(false).AnyAsync(u => u.StudentNumber == number && u.Id != user.Id))
                return Error.DuplicateStudentNumber;

            state.Data = JsonSerializer.Serialize(new[] { number });
            state.Step = 1;
            await repository.SaveChangesAsync();
            return RegistrationPrompt(state);
        }

        var pending = JsonSerializer.Deserialize<string[]>(state.Data) ?? [];
        var result = await mediator.Send(new RegisterStudentCommand(user.TelegramId,
            pending.FirstOrDefault() ?? user.StudentNumber!, input));
        if (!result.IsSuccessful) return result.Message;

        return result.Message + "\n\n" + await AfterRegistration(user);
    }

    private async Task<string> AfterRegistration(User user)
    {
        if (!user.IsAdmin && !await repository.Query<CourseMembership>().AnyAsync(m => m.UserId == user.Id))
        {
            await StartFlow(user.Id, "select_class");
            return await ClassSelectionPrompt();
        }

        await ClearFlow(user.Id);
        return Home();
    }

    private static string Home() => Messages.HomeHeading + Messages.HomeMenuHint;

    private static string SelectionKey(Conversation state)
    {
        return (state.Flow == "word" ? "w" : state.Flow == "browse" ? "b" : "j") + state.Id.ToString("N")[..8];
    }

    private async Task<string> Back(User user)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state is null) return Home();
        var values = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
        if (state.Flow == "register") return await Register(user, state, "/back");
        if (state.Flow == "course" && values.Count > 0)
        {
            values.RemoveAt(values.Count - 1);
            state.Step = values.Count;
            state.Data = JsonSerializer.Serialize(values);
            await repository.SaveChangesAsync();
            return CoursePrompts[state.Step];
        }

        if (state.Flow == "word" && values.Count > 0)
        {
            if (values.Count == 1 &&
                await repository.Query<CourseMembership>().CountAsync(m => m.UserId == user.Id) <= 1)
                return global::VocabularyBot.Infrastructure.Resources.Messages.FirstWordStep + WordPrompts[1];
            var previous = Math.Min(values.Count - 1, 5);
            values.RemoveRange(previous, values.Count - previous);
            state.Step = values.Count;
            state.Data = JsonSerializer.Serialize(values);
            await repository.SaveChangesAsync();
            return (previous == 5 ? global::VocabularyBot.Infrastructure.Resources.Messages.ReenterWordDetails : "") + WordPrompts[state.Step];
        }

        return global::VocabularyBot.Infrastructure.Resources.Messages.FirstStepCancelHint;
    }

    public async Task<BotKeyboard> Keyboard(long telegramId, string input)
    {
        var user = await repository.Query<User>(false).FirstOrDefaultAsync(x => x.TelegramId == telegramId);
        if (user is null) return BotMenu.Reply(["/start"]);
        var state = await repository.Query<Conversation>(false).FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (!HasProfile(user) || state?.Flow == "register")
        {
            var canGoBack = state?.Step == 1 && state.Data != "[]";
            var rows = new List<string[]>();
            if (canGoBack) rows.Add([BotMenu.Back]);
            if (HasProfile(user)) rows.Add([BotMenu.Cancel]);
            return BotMenu.Reply(rows.ToArray()) with
            {
                Placeholder = state?.Step == 1 ? Messages.FullNamePlaceholder : Messages.StepInputPlaceholder
            };
        }

        if (state is not null)
        {
            if (state.Flow == "browse")
            {
                const int pageSize = 8;
                var parts = input.Split(':');
                var page = parts.Length == 4 && parts[0] == "browse" && parts[1] == SelectionKey(state) &&
                           parts[2] == "page" && int.TryParse(parts[3], out var requested)
                    ? Math.Max(0, requested)
                    : 0;
                var rows = new List<IReadOnlyList<BotButton>>();
                int count;
                if (state.Step == 0)
                {
                    var courses = repository.Query<Course>(false);
                    if (!user.IsAdmin)
                        courses = courses.Where(c =>
                            repository.Query<CourseMembership>(false)
                                .Any(m => m.CourseId == c.Id && m.UserId == user.Id));
                    var query = courses.OrderBy(c => c.University).ThenBy(c => c.Year).ThenBy(c => c.Term)
                        .ThenBy(c => c.Id);
                    count = await query.CountAsync();
                    page = Math.Min(page, Math.Max(0, (count - 1) / pageSize));
                    foreach (var course in await query.Skip(page * pageSize).Take(pageSize).ToListAsync())
                        rows.Add([
                            new BotButton(string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.CourseButtonLabel, course.University, course.Year, course.Term),
                                $"browse:{SelectionKey(state)}:course:{course.Id:N}")
                        ]);
                }
                else
                {
                    var data = JsonSerializer.Deserialize<List<string>>(state.Data) ?? [];
                    var courseId = Guid.Parse(data[0]);
                    var query = repository.Query<User>(false)
                        .Where(u => u.StudentNumber != null && repository.Query<CourseMembership>(false)
                            .Any(m => m.CourseId == courseId && m.UserId == u.Id)).OrderBy(u => u.FullName == null)
                        .ThenBy(u => u.FullName).ThenBy(u => u.StudentNumber)
                        .ThenBy(u => u.Id);
                    count = await query.CountAsync();
                    page = Math.Min(page, Math.Max(0, (count - 1) / pageSize));
                    foreach (var student in await query.Skip(page * pageSize).Take(pageSize).ToListAsync())
                        rows.Add([
                            new BotButton(StudentLabel(student),
                                $"browse:{SelectionKey(state)}:student:{student.Id:N}")
                        ]);
                }

                var nav = new List<BotButton>();
                if (page > 0) nav.Add(new BotButton(global::VocabularyBot.Infrastructure.Resources.Messages.PreviousPage, $"browse:{SelectionKey(state)}:page:{page - 1}"));
                if ((page + 1) * pageSize < count)
                    nav.Add(new BotButton(global::VocabularyBot.Infrastructure.Resources.Messages.NextPage, $"browse:{SelectionKey(state)}:page:{page + 1}"));
                if (nav.Count > 0) rows.Add(nav);
                if (state.Step == 1)
                {
                    rows.Add([new BotButton(global::VocabularyBot.Infrastructure.Resources.Messages.AllCourseWords, $"browse:{SelectionKey(state)}:all:0")]);
                    rows.Add([new BotButton(global::VocabularyBot.Infrastructure.Resources.Messages.CourseListButton, $"browse:{SelectionKey(state)}:classes:0")]);
                }

                if (rows.Count == 0) return BotMenu.Reply([BotMenu.Join, BotMenu.Home]);
                return new BotKeyboard(rows, true);
            }

            if (state.Flow == "select_class" || (state.Flow == "word" && state.Step == 0))
            {
                var query = repository.Query<Course>(false);
                if (state.Flow == "word")
                    query = query.Where(c =>
                        repository.Query<CourseMembership>(false).Any(m => m.CourseId == c.Id && m.UserId == user.Id));
                const int pageSize = 8;
                var count = await query.CountAsync();
                var parts = input.Split(':');
                var page = parts.Length == 3 && parts[0] == "page" && parts[1] == SelectionKey(state) &&
                           int.TryParse(parts[2], out var requested)
                    ? Math.Clamp(requested, 0, Math.Max(0, (count - 1) / pageSize))
                    : 0;
                var courses = await query.OrderBy(c => c.University).ThenBy(c => c.Year).ThenBy(c => c.Term)
                    .ThenBy(c => c.Id)
                    .Skip(page * pageSize).Take(pageSize).ToListAsync();
                if (courses.Count == 0) return BotMenu.Reply([BotMenu.Join], [BotMenu.Cancel]);
                var rows = courses.Select(c => (IReadOnlyList<BotButton>)new[]
                {
                    new BotButton(string.Format(global::VocabularyBot.Infrastructure.Resources.Messages.CourseButtonLabel, c.University, c.Year, c.Term), $"class:{SelectionKey(state)}:{c.Id:N}")
                }).ToList();
                var navigation = new List<BotButton>();
                if (page > 0) navigation.Add(new BotButton(global::VocabularyBot.Infrastructure.Resources.Messages.PreviousPage, $"page:{SelectionKey(state)}:{page - 1}"));
                if ((page + 1) * pageSize < count)
                    navigation.Add(new BotButton(global::VocabularyBot.Infrastructure.Resources.Messages.NextPage, $"page:{SelectionKey(state)}:{page + 1}"));
                if (navigation.Count > 0) rows.Add(navigation);
                rows.Add([new BotButton(BotMenu.Cancel, $"class:{SelectionKey(state)}:cancel")]);
                return new BotKeyboard(rows, true);
            }

            var controls = new List<string[]>();
            if (state.Flow == "word" && state.Step == 4) controls.Add([BotMenu.SkipFamily]);
            if (state.Flow == "word" && state.Step == 5) controls.Add([BotMenu.Skip]);
            if (state.Flow == "word" && state.Step >= 6) controls.Add([BotMenu.Done]);
            var canGoBack = (state.Flow == "course" && state.Step > 0) ||
                            (state.Flow == "word" && (state.Step > 1 || (state.Step == 1 &&
                                                                         await repository.Query<CourseMembership>(false)
                                                                             .CountAsync(m => m.UserId == user.Id) >
                                                                         1)));
            if (canGoBack) controls.Add([BotMenu.Back, BotMenu.Cancel]);
            else controls.Add([BotMenu.Cancel]);
            controls.Add([BotMenu.Help]);
            return BotMenu.Reply(controls.ToArray()) with { Placeholder = global::VocabularyBot.Infrastructure.Resources.Messages.StepInputPlaceholder };
        }

        var menu = new List<string[]>();
        menu.Add([BotMenu.Word, BotMenu.Words]);
        menu.Add([BotMenu.Classes, BotMenu.Join]);
        menu.Add([BotMenu.Register, BotMenu.Help]);
        if (user.IsAdmin)
        {
            menu.Add([BotMenu.Course, BotMenu.Admin]);
            menu.Add([BotMenu.Browse]);
        }

        return BotMenu.Reply(menu.ToArray());
    }

    private async Task<Conversation> StartFlow(Guid userId, string flow, int step = 0)
    {
        // A fresh conversation identity also expires buttons from previous forms.
        await ClearFlow(userId);
        var state = new Conversation { UserId = userId, Flow = flow, Step = step };
        repository.Create(state);
        await repository.SaveChangesAsync();
        return state;
    }

    private async Task ClearFlow(Guid userId)
    {
        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == userId);
        if (state is not null)
        {
            repository.Delete(state);
            await repository.SaveChangesAsync();
        }
    }
}
