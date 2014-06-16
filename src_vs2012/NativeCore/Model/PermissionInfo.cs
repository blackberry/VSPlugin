using System;
using System.Collections.Generic;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Descriptor of permissions, that are allowed to be set by the application compiled against specified NDK.
    /// </summary>
    public sealed class PermissionInfo
    {
        public PermissionInfo(string id, string name, string description)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            ID = id;
            Name = name;
            Description = description ?? string.Empty;
        }

        #region Properties

        public string ID
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        #endregion

        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// Creates the default set of permissions for BlackBerry 10 application.
        /// </summary>
        public static PermissionInfo[] CreateDefaultList()
        {
            var result = new List<PermissionInfo>();

            result.Add(new PermissionInfo("bbm_connect", "BlackBerry Messenger", "Allows this app to connect to the BBM Social Platform to access BBM contact lists and user profiles, invite BBM contacts to download your app, initiate BBM chats and share content from within your app, or stream data between apps in real time."));
            result.Add(new PermissionInfo("access_pimdomain_calendars", "Calendar", "Allows this app to access the calendar on the device. This access includes viewing, adding, and deleting calendar appointments."));
            result.Add(new PermissionInfo("use_camera", "Camera", "Allows this app to take pictures, record video, and use the flash."));
            result.Add(new PermissionInfo("use_camera_desktop", "Capture Screen", "Allows this app to take screen shots, share your screen, and take videos of your screen."));
            result.Add(new PermissionInfo("access_pimdomain_contacts", "Contacts", "Allows this app to access the contacts stored on the device. This access includes viewing, creating, and deleting the contacts."));
            result.Add(new PermissionInfo("read_device_identifying_information", "Device Identifying Information", "Allows this app to access device identifiers such as serial number and PIN."));
            result.Add(new PermissionInfo("access_pimdomain_messages", "Email and PIN Message", "Allows this app to access the email and PIN messages stored on the device. This access includes viewing, creating, sending, and deleting the messages."));
            result.Add(new PermissionInfo("use_gamepad", "Gamepad", "Allows this app to support platform Gamepad functionality."));
            result.Add(new PermissionInfo("access_internet", "Internet", "Allows this app to use Wi-fi, wired, or other connections to a destination that is not local on the user's device."));
            result.Add(new PermissionInfo("access_location_services", "Location", "Allows this app to access the device’s current or saved locations."));
            result.Add(new PermissionInfo("record_audio", "Microphone", "Allows this app to record sound using the microphone."));
            result.Add(new PermissionInfo("read_personally_identifiable_information", "My Contact Info", "Allows the app to access your contact info, such as your name (where available), and the email address you use as your BlackBerry ID username."));
            result.Add(new PermissionInfo("access_pimdomain_notebooks", "Notebooks", "Allows this app to access the content stored in the notebooks on the device. This access includes adding and deleting entries and content."));
            result.Add(new PermissionInfo("access_notify_settings_control", "Notification Control", "Allows this app to modify global notification settings."));
            result.Add(new PermissionInfo("access_phone", "Phone", "Determine when a user is on a phone call. This access includes access to the phone number assigned to the device and the BlackBerry ID of the user."));
            result.Add(new PermissionInfo("control_phone", "Phone Control", "Allows this app to merge calls, use the dialpad during calls, or end calls."));
            result.Add(new PermissionInfo("post_notification", "Post Notifications", "Post a notification to the notifications area of the screen."));
            result.Add(new PermissionInfo("_sys_use_consumer_push", "Consumer Push", "Allows this app to use the Push Service with the BlackBerry Internet Service. This access allows the app to receive and request push messages. To use the Push Service with the BlackBerry Internet Service, you must register with Research In Motion. When you register, you receive a confirmation email message that contains information that your application needs to receive and request push messages. For more information about registering, visit https://developer.blackberry.com/services/push/. If you're using the Push Service with the BlackBerry Enterprise Server or the BlackBerry Device Service, you don't need to register with BlackBerry."));
            result.Add(new PermissionInfo("_sys_run_headless", "Run in Background", "Allows an app to run as a invokable headless service without a UI. By default the service runs for a finite amount of time."));
            result.Add(new PermissionInfo("_sys_run_headless_nostop", "Run in Background Continuously", "Allows an app to run as a long running headless service. Used in conjunction with _sys_run_headless."));
            result.Add(new PermissionInfo("run_when_backgrounded", "Run as Active Frame", "Allows background processing. Without this permission, the app is stopped when the user switches focus to another app. Apps that use this permission are rigorously reviewed for acceptance to BlackBerry App World storefront for their use of power."));
            result.Add(new PermissionInfo("access_shared", "Shared Files", "Allows this app to access pictures, music, documents, and other files stored on the user's device, at a remote storage provider, on a media card, or in the cloud."));
            result.Add(new PermissionInfo("access_sms_mms", "Text Messages", "Allows this app to access the text messages stored on the device. The access includes viewing, creating, sending, and deleting text messages."));
            result.Add(new PermissionInfo("narrow_landscape_exit", "Narrow Swipe Up", "Disables the application close gesture. Designed for games."));
            result.Add(new PermissionInfo("access_wifi_public", "WiFi Connection", "Allows the app to receive Wi-Fi event notifications such as Wi-Fi scan results or changes in the Wi-Fi connection state. This permission also allows limited Wi-Fi control for hotspot aggregator applications that manage network selection and the authentication to a Wi-Fi Hotspot. This permission does not allow the app the ability to force a connection to a specific network profile when there are other available networks that have a higher priority configured on the device. It is not necessary to configure this permission if you only want to retrieve or query information about existing Wi-Fi connections."));

            return result.ToArray();
        }

        /// <summary>
        /// Creates a list of permissions designed for PlayBook applications.
        /// </summary>
        public static PermissionInfo[] CreatePlayBookList()
        {
            var result = new List<PermissionInfo>();

            result.Add(new PermissionInfo("access_shared", "Files", "Read and write files that are shared between all applications run by the current user."));
            result.Add(new PermissionInfo("record_audio", "Microphone", "Access the audio stream from the microphone."));
            result.Add(new PermissionInfo("read_geolocation", "GPS Location", "Allows this app to access the current GPS location of the device."));
            result.Add(new PermissionInfo("use_camera", "Camera", "Capture images and video using the cameras."));
            result.Add(new PermissionInfo("access_internet", "Internet", "Use a Wi-Fi, wired, or other connection to a destination that is not local."));
            result.Add(new PermissionInfo("play_audio", "Play Sounds", "Play an audio stream."));
            result.Add(new PermissionInfo("post_notification", "Post Notification", "Post a notification to the notifications area of the screen."));
            result.Add(new PermissionInfo("set_audio_volume", "Set Audio Volume", "Change the volume of an audio stream being played."));
            result.Add(new PermissionInfo("read_device_identifying_information", "Device Identifying Information", "Access unique device identifying information (e.g. PIN)."));

            return result.ToArray();
        }
    }
}
