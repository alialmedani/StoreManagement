-- حذف Mobile Client القديم وإعادة إنشاءه
DELETE FROM [OpenIddictAuthorizations] WHERE ApplicationId IN (SELECT Id FROM [OpenIddictApplications] WHERE ClientId = 'StoreManagement_Mobile');
DELETE FROM [OpenIddictTokens] WHERE ApplicationId IN (SELECT Id FROM [OpenIddictApplications] WHERE ClientId = 'StoreManagement_Mobile');
DELETE FROM [OpenIddictApplications] WHERE ClientId = 'StoreManagement_Mobile';

-- سيتم إعادة إنشاء الـ client تلقائياً عند تشغيل DbMigrator

