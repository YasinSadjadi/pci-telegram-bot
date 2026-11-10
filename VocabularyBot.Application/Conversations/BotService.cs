using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VocabularyBot.Application.Queries.GetStatus;
using VocabularyBot.Application.Queries.ListCourses;
using VocabularyBot.Application.Queries.ListGroups;
using VocabularyBot.Application.Queries.ListUsers;
using VocabularyBot.Application.Queries.ListWeeks;
using VocabularyBot.Application.Workflows.AssignAdmin;
using VocabularyBot.Application.Workflows.CreateCourse;
using VocabularyBot.Application.Workflows.CreateGroup;
using VocabularyBot.Application.Workflows.CreateWeek;
using VocabularyBot.Application.Workflows.ManageMembership;
using VocabularyBot.Application.Workflows.RegisterWord;
using VocabularyBot.Domain;
using VocabularyBot.Infrastructure.Abstractions.Results;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Application.Conversations;

public sealed class BotService(IGenericRepository repository, IMediator mediator, BotOptions options)
{
    private const string Help =
        "دستورها:\n/start ثبت‌نام و نمایش شناسه\n/classes فهرست کلاس‌ها\n/groups فهرست گروه‌های شما\n/weeks فهرست هفته‌های کلاس‌های شما\n/word ثبت واژه\n/cancel لغو عملیات\n\nادمین: /course ایجاد کلاس، /group ایجاد گروه، /week ایجاد هفته، /users فهرست کاربران، /member افزودن عضو، /leader تعیین سرگروه، /admin اعطای نقش، /status گزارش";

    private static readonly Dictionary<string, string[]> Prompts = new()
    {
        ["course"] = ["نام دانشگاه را وارد کنید:", "سال ترم را وارد کنید (مثلاً ۴۰۴):", "شماره ترم را وارد کنید:"],
        ["group"] = ["شناسه کلاس را وارد کنید:", "شماره گروه را وارد کنید:"],
        ["week"] =
        [
            "شناسه کلاس را وارد کنید:", "عنوان هفته را وارد کنید:", "شروع هفته به صورت yyyy-MM-dd HH:mm (ساعت تهران):",
            "پایان هفته به صورت yyyy-MM-dd HH:mm (ساعت تهران):", "ساعت یادآوری در روز پایانی به صورت HH:mm:"
        ],
        ["member"] = ["شناسه گروه را وارد کنید:", "شناسه عددی یا نام کاربری عضو ثبت‌نام‌شده را وارد کنید:"],
        ["leader"] = ["شناسه گروه را وارد کنید:", "شناسه عددی یا نام کاربری عضو گروه را وارد کنید:"],
        ["admin"] = ["شناسه عددی یا نام کاربری کاربر ثبت‌نام‌شده را وارد کنید:"],
        ["word"] =
        [
            "شناسه گروه خود را وارد کنید:", "شناسه هفته فعال را وارد کنید:", "عنوان واژه یا عبارت:", "معادل فارسی:",
            "تعریف انگلیسی:", "مثال:", "واژه‌های مرتبط:", "منبع:"
        ]
    };

    public async Task<string> Handle(long id, string? username, string name, string input)
    {
        input = input.Trim();
        var user = await repository.Query<User>().FirstOrDefaultAsync(x => x.TelegramId == id);
        if (input == "/start")
        {
            if (user == null)
            {
                user = new User { TelegramId = id };
                repository.Create(user);
            }

            user.Name = name;
            user.Username = username;
            if (id == options.BootstrapAdminId) user.IsAdmin = true;
            await repository.SaveChangesAsync();
            return $"خوش آمدید. شناسه شما: {id}\n{Help}";
        }

        if (user == null) return "ابتدا /start را ارسال کنید.";
        if (input == "/cancel")
        {
            var c = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (c != null)
            {
                repository.Delete(c);
                await repository.SaveChangesAsync();
            }

            return "عملیات لغو شد.";
        }

        if (input is "/help" or "/menu") return Help;
        if (input == "/users") return await mediator.Send(new ListUsersQuery(id));
        if (input == "/classes") return await mediator.Send(new ListCoursesQuery(id));
        if (input == "/groups") return await mediator.Send(new ListGroupsQuery(id));
        if (input == "/weeks") return await mediator.Send(new ListWeeksQuery(id));
        if (input == "/status") return await mediator.Send(new GetStatusQuery(id));

        if (input.StartsWith('/'))
        {
            var flow = input[1..].Split(' ')[0].ToLowerInvariant();
            if (!Prompts.ContainsKey(flow)) return "دستور ناشناخته است. /help";
            if (flow != "word" && !user.IsAdmin) return "دسترسی ندارید.";
            var old = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (old != null) repository.Delete(old);
            await repository.SaveChangesAsync();
            repository.Create(new Conversation { UserId = user.Id, Flow = flow });
            await repository.SaveChangesAsync();
            return Prompts[flow][0] + "\nبرای لغو /cancel";
        }

        var state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (state == null) return "برای شروع /help را بفرستید.";
        if (state.Flow != "word" && !user.IsAdmin)
        {
            repository.Delete(state);
            await repository.SaveChangesAsync();
            return "دسترسی ندارید.";
        }

        if (input.Length == 0 || input.Length > 2000) return "متن باید بین ۱ تا ۲۰۰۰ نویسه باشد.";
        var values = JsonSerializer.Deserialize<List<string>>(state.Data)!;
        values.Add(input);
        if (values.Count < Prompts[state.Flow].Length)
        {
            state.Data = JsonSerializer.Serialize(values);
            state.Step = values.Count;
            await repository.SaveChangesAsync();
            return Prompts[state.Flow][state.Step];
        }

        try
        {
            var result = state.Flow switch
            {
                "admin" => await mediator.Send(new AssignAdminCommand(id, values)),
                "course" => await mediator.Send(new CreateCourseCommand(id, values)),
                "group" => await mediator.Send(new CreateGroupCommand(id, values)),
                "week" => await mediator.Send(new CreateWeekCommand(id, values)),
                "member" => await mediator.Send(new ManageMembershipCommand(id, values, false)),
                "leader" => await mediator.Send(new ManageMembershipCommand(id, values, true)),
                "word" => await mediator.Send(new RegisterWordCommand(id, values)),
                _ => Result.Failure("عملیات ناشناخته است.")
            };
            if (!result.IsSuccessful)
            {
                repository.ClearChanges();
                state = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            }

            if (state != null) repository.Delete(state);
            await repository.SaveChangesAsync();
            return result.Message;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException
                                       or DbUpdateException)
        {
            repository.ClearChanges();
            var s = await repository.Query<Conversation>().FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (s != null)
            {
                repository.Delete(s);
                await repository.SaveChangesAsync();
            }

            return "عملیات انجام نشد: " + (ex is DbUpdateException ? "اطلاعات تکراری یا نامعتبر است." : ex.Message) +
                   "\nلطفاً دوباره شروع کنید.";
        }
    }
}