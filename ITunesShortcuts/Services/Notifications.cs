using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ITunesShortcuts.Services;

public class Notifications
{
    public const string Action = "action";
    public const string Group = "group";
    public const string Content = "content";

    public const string ViewAction = "view";
    public const string ButtonAction = "button";
    public const string ComboBoxAction = "comboBox";


    readonly ILogger<Notifications> logger;
    readonly WindowHelper windowHelper;

    readonly AppNotificationManager notificationManager = AppNotificationManager.Default;
    readonly Dictionary<string, Action<string>> groupActionMapping = new();

    public Notifications(
        ILogger<Notifications> logger,
        WindowHelper windowHelper)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;

        logger.LogInformation("[Notifications-.ctor] Notifications has been initialized.");
    }

    
    public void Register()
    {
        notificationManager.NotificationInvoked += (s, args) => Handle(args);
        notificationManager.Register();

        AppActivationArguments activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activatedArgs.Kind == ExtendedActivationKind.AppNotification)
            Handle((AppNotificationActivatedEventArgs)activatedArgs.Data);

        logger.LogInformation("[Notifications-Register] Notifications has been initialized.");

    }

    public void Unregister()
    {
        notificationManager.UnregisterAll();
        groupActionMapping.Clear();
    }


    public void Handle(
        AppNotificationActivatedEventArgs args) =>
        windowHelper.EnqueDispatcher(() =>
        {
            if (!args.Arguments.TryGetValue(Action, out string? actionType) || actionType is null)
            {
                logger.LogError("[Notifications-Handle] Notification could not be handled: action key was not found.");
                return;
            }

            switch (actionType)
            {
                case ViewAction:
                    windowHelper.SetVisibility(true);
                    break;

                case ButtonAction:
                    HandleButtonAction(args.Arguments);
                    break;
                case ComboBoxAction:
                    HandleComboBoxAction(args.Arguments, args.UserInput);
                    break;
            }

            logger.LogInformation("[Notifications-Handle] Notification was handled: {actionType}.", actionType);
        });

    void HandleButtonAction(
        IDictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue(Group, out string? group) || group is null ||                    // if no group was set
            !arguments.TryGetValue(Content, out string? content) || content is null ||              // if no content was set
            !groupActionMapping.TryGetValue(group, out Action<string>? action) || action is null)   // if no action was found with group
        {
            logger.LogError("[Notifications-Handle] Notification could not be handled: group key, content key or action was not found.");
            return;
        }

        action.Invoke(content);
        groupActionMapping.Remove(group);
    }

    void HandleComboBoxAction(
        IDictionary<string, string> arguments,
        IDictionary<string, string> userInputs)
    {
        if (!arguments.TryGetValue(Group, out string? group) || group is null ||                    // if no group was set
            !userInputs.TryGetValue(group, out string? input) || input is null ||                   // if not user input was given
            !groupActionMapping.TryGetValue(group, out Action<string>? action) || action is null)   // if no action was found with group
        {
            logger.LogError("[Notifications-HandleComboBoxAction] Notification could not be handled: group key, user input or action was not found.");
            return;
        }

        action.Invoke(input);
        groupActionMapping.Remove(group);
    }


    public void Send(
        AppNotificationBuilder builder)
    {
        AppNotification notification = builder
            .AddArgument(Action, ViewAction)
            .BuildNotification();

        notificationManager.Show(notification);
        logger.LogInformation("[Notifications-Send] Notification was sent.");
    }

    public void SendWithButton(
        string[] buttons,
        Action<string> buttonAction,
        AppNotificationBuilder? builder = null)
    {
        builder ??= new();

        string group = Guid.NewGuid().ToString();
        groupActionMapping.Add(group, buttonAction);

        foreach (string button in buttons)
        {
            builder.AddButton(new AppNotificationButton(button)
                .AddArgument(Action, ButtonAction)
                .AddArgument(Group, group)
                .AddArgument(Content, button));
        }

        Send(builder);
    }

    public void SendWithComboBox(
        string[] items,
        Action<string?> selectAction,
        string submitButtonText = "Okay",
        AppNotificationBuilder? builder = null)
    {
        builder ??= new();

        string group = Guid.NewGuid().ToString();
        groupActionMapping.Add(group, selectAction);

        AppNotificationComboBox comboBox = new(group);
        foreach (string item in items)
        {
            comboBox.AddItem(item, item);
        }
        builder.AddComboBox(comboBox)
            .AddButton(new AppNotificationButton(submitButtonText)
                .AddArgument(Action, ComboBoxAction)
                .AddArgument(Group, group));

        Send(builder);
    }
}