IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PasswordSelfService_Log')
BEGIN
CREATE TABLE [dbo].[PasswordSelfService_Log](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [ntext] NOT NULL,
	[ip] [nvarchar](256) NOT NULL,
	[mobile] [nvarchar](256) NOT NULL,
	[SMSCode] [nvarchar](256) NOT NULL,
	[Attempted] [datetime] NOT NULL,
	[SamAccountName] [nvarchar](256) NOT NULL,
 CONSTRAINT ["PK_PasswordSelfService_Log"] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
