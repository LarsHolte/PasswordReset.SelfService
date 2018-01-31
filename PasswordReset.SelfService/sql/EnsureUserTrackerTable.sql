IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PasswordSelfService_User_Tracker')
BEGIN
CREATE TABLE [dbo].[PasswordSelfService_User_Tracker](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ip] [nvarchar](256) NOT NULL,
	[mobile] [nvarchar](256) NOT NULL,
	[SMSCode] [nvarchar](256) NOT NULL,
	[Step1Counter] [int] NOT NULL,
	[Step2Counter] [int] NOT NULL,
	[Attempted] [datetime] NOT NULL,
	[SessionID] [uniqueidentifier] NOT NULL,
	[UsedSMSCode] [bit] NOT NULL,
	[SamAccountName] [nvarchar](256) NOT NULL,
 CONSTRAINT ["PK_PasswordSelfService_User_Tracker"] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

END

