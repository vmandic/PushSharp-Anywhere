CREATE DATABASE PushSharpDEMO

GO
USE PushSharpDemo

GO
CREATE TABLE Client
(
	ID int PRIMARY KEY IDENTITY(1,1) NOT NULL,
	Username nvarchar(max) NOT NULL
)

GO
CREATE TABLE [dbo].[MobileDevice](
	[ID] [int] PRIMARY KEY IDENTITY(1,1) NOT NULL,
	[ClientID] [int] FOREIGN KEY REFERENCES Client(ID) NOT NULL,
	[SmartphonePlatform] [nvarchar](max) NULL,
	[PushNotificationsRegistrationID] [nvarchar](max) NULL,
	[Active] [bit] NOT NULL,
	[ModifiedAt] [datetime] NULL,
	[CreatedAt] [datetime] NULL,
	[DeviceID] [nvarchar](max) NULL
)

GO
CREATE TABLE [dbo].[PushNotification](
	[ID] [int] PRIMARY KEY IDENTITY(1,1) NOT NULL,
	[MobileDeviceID] [int] FOREIGN KEY REFERENCES MobileDevice(ID) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[Status] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ModifiedAt] [datetime] NOT NULL,
	[Description] [nvarchar](max) NULL
)

GO
INSERT INTO Client VALUES ('pperic'), ('vmandic'), ('icepodulja')

