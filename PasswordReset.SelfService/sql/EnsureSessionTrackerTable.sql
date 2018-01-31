IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PasswordSelfService_Session_Tracker')
BEGIN
CREATE TABLE [dbo].[PasswordSelfService_Session_Tracker](
	[uniqueID] [uniqueidentifier] NOT NULL,
	[ip] [nvarchar](256) NOT NULL,
	[Attempted] [datetime] NOT NULL,
	[Counter] [int] NOT NULL
 CONSTRAINT ["PK_PasswordSelfService_Session_Tracker"] PRIMARY KEY CLUSTERED 
(
	[uniqueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

END
