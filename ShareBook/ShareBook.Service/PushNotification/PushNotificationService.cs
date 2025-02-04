﻿using Microsoft.Extensions.Options;
using OneSignal.RestAPIv3.Client;
using OneSignal.RestAPIv3.Client.Resources;
using OneSignal.RestAPIv3.Client.Resources.Notifications;
using Rollbar;
using ShareBook.Domain;
using ShareBook.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ShareBook.Service.Notification;

public class PushNotificationService : IPushNotificationService
{
    private readonly PushNotificationSettings _settings;
    private readonly OneSignalClient _oneSignalClient;

    public PushNotificationService(IOptions<PushNotificationSettings> pushNotificationSettings)
    {
        _settings = pushNotificationSettings.Value;
        _oneSignalClient = new OneSignalClient(_settings.ApiKey);
    }

    public string SendNotificationSegments(NotificationOnesignal notficationSettings)
    {
        if (!_settings.IsActive) return "";

        var notificationCreateOptions = new NotificationCreateOptions
        {
            AppId = new Guid(_settings.AppId)
        };

        notificationCreateOptions.IncludedSegments = new List<string>()
        {
            GetSegments(notficationSettings.TypeSegments)
        };

        notificationCreateOptions.Headings.Add(LanguageCodes.Portuguese, notficationSettings.Title);
        notificationCreateOptions.Contents.Add(LanguageCodes.Portuguese, notficationSettings.Content);

        _oneSignalClient.Notifications.Create(notificationCreateOptions);

        return "Enviado com sucesso";
    }

    public string SendNotificationByKey(NotificationOnesignal notficationSettings)
    {
        if (!_settings.IsActive) return "";

        var notificationCreateOptions = new NotificationCreateOptions
        {
            AppId = new Guid(_settings.AppId)
        };

        notificationCreateOptions.Filters = new List<INotificationFilter>
        {
            new NotificationFilterField { Field = NotificationFilterFieldTypeEnum.Tag, Key = notficationSettings.Key, Value = notficationSettings.Value}
        };

        notificationCreateOptions.Headings.Add(LanguageCodes.English, notficationSettings.Title);
        notificationCreateOptions.Contents.Add(LanguageCodes.English, notficationSettings.Content);

        notificationCreateOptions.Headings.Add(LanguageCodes.Portuguese, notficationSettings.Title);
        notificationCreateOptions.Contents.Add(LanguageCodes.Portuguese, notficationSettings.Content);

        _oneSignalClient.Notifications.Create(notificationCreateOptions);

        return $"Notification enviado para o {notficationSettings.Value} com sucesso";
    }

    public string SendNotificationByEmail(string email, string title, string content)
    {
        if (!_settings.IsActive) return "";

        try
        {
            var notificationCreateOptions = new NotificationCreateOptions
            {
                AppId = new Guid(_settings.AppId)
            };

            notificationCreateOptions.Filters = new List<INotificationFilter>
            {
                new NotificationFilterField { Field = NotificationFilterFieldTypeEnum.Tag, Key = "email", Value = email}
            };

            notificationCreateOptions.Headings.Add(LanguageCodes.English, title);
            notificationCreateOptions.Contents.Add(LanguageCodes.English, content);

            notificationCreateOptions.Headings.Add(LanguageCodes.Portuguese, title);
            notificationCreateOptions.Contents.Add(LanguageCodes.Portuguese, content);

            _oneSignalClient.Notifications.Create(notificationCreateOptions);

            return $"Notification enviado para o {email} com sucesso";
        }
        catch (Exception ex)
        {
            RollbarLocator.RollbarInstance.Error(ex);
            return "";
        }
    }

    private static string GetSegments(TypeSegments typeSegments)
    {
        switch (typeSegments)
        {
            case TypeSegments.Inactive:
                return "Inactive Users";

            case TypeSegments.Engaged:
                return "Engaged Users";

            case TypeSegments.All:
                return "Subscribed Users";

            case TypeSegments.Active:
                return "Active Users";

            default:
                return "";
        }
    }
}