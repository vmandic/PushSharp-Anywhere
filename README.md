Warning notice
===============
This project has not been updated for a long time, proceed with caution and give starts to <a href="https://github.com/redth">@redth</a> for making this amazing library. :)

PushSharp-Anywhere
===============

This project demonstrates the usage/implementation of the PushSharp .NET SDK for simple and efficient management of multiplatform (IOS, Android and Windows Phone) Push Notification services on the server side. 

This solution is consisted from a couple of separate projects including shared processor libraries; a Web .NET WebAPI    app and a Desktop Windows Forms app used for monitoring and handling push notifications as a "state machine".

The repo is also planned to have more (iOS Obj-C, Android, Win RT 8.1, Win10 UWP, Xamarin.iOS, Xamarin.Android, Xamarin.Forms) client implementations connecting to an PushSharp service server.

ATTENTION: Take care of how much PushNotification objects you will read from the database when processing, in this case there is a thread looping/pooling the database for one per one of the newst push notifications. You could also set to read all the notifications instead of one (latest). For more info jump into the code of the PushNotificationProcessor.cs file.

Instruction for use
===============
Server: Enter all connection strings to your database where needed, register for push notifications on the service providers: Google, Apple and Microsoft, enter the provided keys to PushNotificationProcessor.cs.

Clients: Enter your 3rd party server endpoints in the proper places/classes.

Disclaimer
===============
This project was built for a show case on the mscommunity.hr standup/meetup in Zagreb held at 25.11.2014. No coded tests were performed, bugs are possible, please contribute and fork on! 

Update 23.9.2015.
===============
Project was updated for the conference MobilityDay 2015 showcase. Take care when loading the Android app, needs gradle reconfiguration.

