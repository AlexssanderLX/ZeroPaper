DROP PROCEDURE IF EXISTS `POMELO_BEFORE_DROP_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `Extra` = 'auto_increment'
			AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS `POMELO_AFTER_ADD_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID INT(11);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
			AND `COLUMN_TYPE` LIKE '%int%'
			AND `COLUMN_KEY` = 'PRI';
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

CREATE TABLE IF NOT EXISTS `__efmigrationshistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___efmigrationshistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE TABLE `Tenants` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `Identifier` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Tenants` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE TABLE `Companies` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `LegalName` varchar(180) CHARACTER SET utf8mb4 NOT NULL,
        `TradeName` varchar(180) CHARACTER SET utf8mb4 NOT NULL,
        `AccessSlug` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
        `DocumentNumber` varchar(30) CHARACTER SET utf8mb4 NULL,
        `ContactEmail` varchar(180) CHARACTER SET utf8mb4 NULL,
        `ContactPhone` varchar(30) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_Companies` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Companies_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE TABLE `Subscriptions` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `PlanName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `MonthlyPrice` decimal(10,2) NOT NULL,
        `MaxUsers` int NOT NULL,
        `StartsAtUtc` datetime(6) NOT NULL,
        `EndsAtUtc` datetime(6) NULL,
        `Status` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_Subscriptions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Subscriptions_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE TABLE `QrCodeAccesses` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Label` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `PublicCode` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `AccessPath` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `IsSingleUse` tinyint(1) NOT NULL,
        `ScanCount` int NOT NULL,
        `LastScanAtUtc` datetime(6) NULL,
        `ExpiresAtUtc` datetime(6) NULL,
        `Status` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_QrCodeAccesses` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_QrCodeAccesses_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_QrCodeAccesses_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE TABLE `Users` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `FullName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(180) CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Role` int NOT NULL,
        `LastLoginAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Users_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Users_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE UNIQUE INDEX `IX_Companies_TenantId_AccessSlug` ON `Companies` (`TenantId`, `AccessSlug`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE INDEX `IX_QrCodeAccesses_CompanyId` ON `QrCodeAccesses` (`CompanyId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE UNIQUE INDEX `IX_QrCodeAccesses_PublicCode` ON `QrCodeAccesses` (`PublicCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE INDEX `IX_QrCodeAccesses_TenantId` ON `QrCodeAccesses` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE INDEX `IX_Subscriptions_TenantId` ON `Subscriptions` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE UNIQUE INDEX `IX_Tenants_Identifier` ON `Tenants` (`Identifier`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE INDEX `IX_Users_CompanyId` ON `Users` (`CompanyId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    CREATE UNIQUE INDEX `IX_Users_TenantId_Email` ON `Users` (`TenantId`, `Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318034028_InitialRestaurantMvp') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260318034028_InitialRestaurantMvp', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE TABLE `DiningTables` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `QrCodeAccessId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `InternalCode` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `Seats` int NOT NULL,
        `Status` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_DiningTables` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_DiningTables_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_DiningTables_QrCodeAccesses_QrCodeAccessId` FOREIGN KEY (`QrCodeAccessId`) REFERENCES `QrCodeAccesses` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_DiningTables_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE TABLE `Sessions` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `AppUserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `TokenHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `ExpiresAtUtc` datetime(6) NOT NULL,
        `LastSeenAtUtc` datetime(6) NULL,
        `RevokedAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_Sessions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Sessions_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Sessions_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Sessions_Users_AppUserId` FOREIGN KEY (`AppUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE TABLE `StockItems` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Category` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Unit` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `CurrentQuantity` decimal(10,2) NOT NULL,
        `MinimumQuantity` decimal(10,2) NOT NULL,
        `LastRestockedAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_StockItems` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_StockItems_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_StockItems_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE TABLE `CustomerOrders` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `DiningTableId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Number` int NOT NULL,
        `CustomerName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `Notes` varchar(600) CHARACTER SET utf8mb4 NULL,
        `Status` int NOT NULL,
        `TotalAmount` decimal(10,2) NOT NULL,
        `SubmittedAtUtc` datetime(6) NOT NULL,
        `SentToKitchenAtUtc` datetime(6) NULL,
        `ReadyAtUtc` datetime(6) NULL,
        `ClosedAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_CustomerOrders` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CustomerOrders_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CustomerOrders_DiningTables_DiningTableId` FOREIGN KEY (`DiningTableId`) REFERENCES `DiningTables` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CustomerOrders_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE TABLE `OrderItems` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Quantity` decimal(10,2) NOT NULL,
        `UnitPrice` decimal(10,2) NOT NULL,
        `Notes` varchar(300) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_OrderItems` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_OrderItems_CustomerOrders_CustomerOrderId` FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrders` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE UNIQUE INDEX `IX_CustomerOrders_CompanyId_Number` ON `CustomerOrders` (`CompanyId`, `Number`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_CustomerOrders_DiningTableId` ON `CustomerOrders` (`DiningTableId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_CustomerOrders_TenantId` ON `CustomerOrders` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE UNIQUE INDEX `IX_DiningTables_CompanyId_InternalCode` ON `DiningTables` (`CompanyId`, `InternalCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE UNIQUE INDEX `IX_DiningTables_QrCodeAccessId` ON `DiningTables` (`QrCodeAccessId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_DiningTables_TenantId` ON `DiningTables` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_OrderItems_CustomerOrderId` ON `OrderItems` (`CustomerOrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_Sessions_AppUserId` ON `Sessions` (`AppUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_Sessions_CompanyId` ON `Sessions` (`CompanyId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_Sessions_TenantId` ON `Sessions` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE UNIQUE INDEX `IX_Sessions_TokenHash` ON `Sessions` (`TokenHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE UNIQUE INDEX `IX_StockItems_CompanyId_Name` ON `StockItems` (`CompanyId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    CREATE INDEX `IX_StockItems_TenantId` ON `StockItems` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318111404_PortalWorkspaceMvp') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260318111404_PortalWorkspaceMvp', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318214642_SignupCodeAccessControl') THEN

    CREATE TABLE `SignupCodes` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `Label` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `CodeHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `BoundEmail` varchar(180) CHARACTER SET utf8mb4 NULL,
        `AllowedPlanName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `AllowedMaxUsers` int NULL,
        `ExpiresAtUtc` datetime(6) NOT NULL,
        `MaxUses` int NOT NULL,
        `UsedCount` int NOT NULL,
        `LastUsedAtUtc` datetime(6) NULL,
        `CreatedByUserId` char(36) COLLATE ascii_general_ci NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_SignupCodes` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318214642_SignupCodeAccessControl') THEN

    CREATE UNIQUE INDEX `IX_SignupCodes_CodeHash` ON `SignupCodes` (`CodeHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260318214642_SignupCodeAccessControl') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260318214642_SignupCodeAccessControl', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319034526_PasswordResetFlow') THEN

    CREATE TABLE `PasswordResetRequests` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `AppUserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `TokenHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `ExpiresAtUtc` datetime(6) NOT NULL,
        `UsedAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_PasswordResetRequests` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PasswordResetRequests_Users_AppUserId` FOREIGN KEY (`AppUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319034526_PasswordResetFlow') THEN

    CREATE INDEX `IX_PasswordResetRequests_AppUserId_IsActive` ON `PasswordResetRequests` (`AppUserId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319034526_PasswordResetFlow') THEN

    CREATE UNIQUE INDEX `IX_PasswordResetRequests_TokenHash` ON `PasswordResetRequests` (`TokenHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319034526_PasswordResetFlow') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260319034526_PasswordResetFlow', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE TABLE `MenuCategories` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_MenuCategories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_MenuCategories_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_MenuCategories_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE TABLE `MenuItems` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `MenuCategoryId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(260) CHARACTER SET utf8mb4 NULL,
        `AccentLabel` varchar(60) CHARACTER SET utf8mb4 NULL,
        `Price` decimal(10,2) NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_MenuItems` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_MenuItems_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_MenuItems_MenuCategories_MenuCategoryId` FOREIGN KEY (`MenuCategoryId`) REFERENCES `MenuCategories` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_MenuItems_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE UNIQUE INDEX `IX_MenuCategories_CompanyId_Name` ON `MenuCategories` (`CompanyId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE INDEX `IX_MenuCategories_TenantId` ON `MenuCategories` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE UNIQUE INDEX `IX_MenuItems_CompanyId_MenuCategoryId_Name` ON `MenuItems` (`CompanyId`, `MenuCategoryId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE INDEX `IX_MenuItems_MenuCategoryId` ON `MenuItems` (`MenuCategoryId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    CREATE INDEX `IX_MenuItems_TenantId` ON `MenuItems` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260319123212_MenuCatalogMvp') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260319123212_MenuCatalogMvp', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320052000_MenuItemImages') THEN

    ALTER TABLE `MenuItems` ADD `ImageUrl` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320052000_MenuItemImages') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260320052000_MenuItemImages', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320220800_OrderCheckoutFlow') THEN

    ALTER TABLE `CustomerOrders` ADD `PaidAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320220800_OrderCheckoutFlow') THEN

    ALTER TABLE `CustomerOrders` ADD `PaymentMethod` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320220800_OrderCheckoutFlow') THEN

    ALTER TABLE `CustomerOrders` ADD `PaymentStatus` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320220800_OrderCheckoutFlow') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260320220800_OrderCheckoutFlow', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320223041_StableOrderSequence') THEN

    ALTER TABLE `Companies` ADD `LastOrderNumber` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320223041_StableOrderSequence') THEN

    UPDATE Companies company
    LEFT JOIN (
        SELECT CompanyId, COALESCE(MAX(Number), 0) AS MaxNumber
        FROM CustomerOrders
        GROUP BY CompanyId
    ) orders ON orders.CompanyId = company.Id
    SET company.LastOrderNumber = COALESCE(orders.MaxNumber, 0);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260320223041_StableOrderSequence') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260320223041_StableOrderSequence', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Companies` DROP FOREIGN KEY `FK_Companies_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `CustomerOrders` DROP FOREIGN KEY `FK_CustomerOrders_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `CustomerOrders` DROP FOREIGN KEY `FK_CustomerOrders_DiningTables_DiningTableId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `CustomerOrders` DROP FOREIGN KEY `FK_CustomerOrders_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `DiningTables` DROP FOREIGN KEY `FK_DiningTables_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `DiningTables` DROP FOREIGN KEY `FK_DiningTables_QrCodeAccesses_QrCodeAccessId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `DiningTables` DROP FOREIGN KEY `FK_DiningTables_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuCategories` DROP FOREIGN KEY `FK_MenuCategories_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuCategories` DROP FOREIGN KEY `FK_MenuCategories_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuItems` DROP FOREIGN KEY `FK_MenuItems_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuItems` DROP FOREIGN KEY `FK_MenuItems_MenuCategories_MenuCategoryId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuItems` DROP FOREIGN KEY `FK_MenuItems_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `OrderItems` DROP FOREIGN KEY `FK_OrderItems_CustomerOrders_CustomerOrderId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `PasswordResetRequests` DROP FOREIGN KEY `FK_PasswordResetRequests_Users_AppUserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `QrCodeAccesses` DROP FOREIGN KEY `FK_QrCodeAccesses_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `QrCodeAccesses` DROP FOREIGN KEY `FK_QrCodeAccesses_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Sessions` DROP FOREIGN KEY `FK_Sessions_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Sessions` DROP FOREIGN KEY `FK_Sessions_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Sessions` DROP FOREIGN KEY `FK_Sessions_Users_AppUserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `StockItems` DROP FOREIGN KEY `FK_StockItems_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `StockItems` DROP FOREIGN KEY `FK_StockItems_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Subscriptions` DROP FOREIGN KEY `FK_Subscriptions_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Users` DROP FOREIGN KEY `FK_Users_Companies_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Users` DROP FOREIGN KEY `FK_Users_Tenants_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'Users');
    ALTER TABLE `Users` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'Tenants');
    ALTER TABLE `Tenants` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'Subscriptions');
    ALTER TABLE `Subscriptions` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'StockItems');
    ALTER TABLE `StockItems` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'SignupCodes');
    ALTER TABLE `SignupCodes` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'Sessions');
    ALTER TABLE `Sessions` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'QrCodeAccesses');
    ALTER TABLE `QrCodeAccesses` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'PasswordResetRequests');
    ALTER TABLE `PasswordResetRequests` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'OrderItems');
    ALTER TABLE `OrderItems` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'MenuItems');
    ALTER TABLE `MenuItems` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'MenuCategories');
    ALTER TABLE `MenuCategories` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'DiningTables');
    ALTER TABLE `DiningTables` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'CustomerOrders');
    ALTER TABLE `CustomerOrders` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'Companies');
    ALTER TABLE `Companies` DROP PRIMARY KEY;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Users` RENAME `users`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Tenants` RENAME `tenants`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Subscriptions` RENAME `subscriptions`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `StockItems` RENAME `stockitems`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `SignupCodes` RENAME `signupcodes`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Sessions` RENAME `sessions`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `QrCodeAccesses` RENAME `qrcodeaccesses`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `PasswordResetRequests` RENAME `passwordresetrequests`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `OrderItems` RENAME `orderitems`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuItems` RENAME `menuitems`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `MenuCategories` RENAME `menucategories`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `DiningTables` RENAME `diningtables`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `CustomerOrders` RENAME `customerorders`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `Companies` RENAME `companies`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `users` RENAME INDEX `IX_Users_TenantId_Email` TO `IX_users_TenantId_Email`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `users` RENAME INDEX `IX_Users_CompanyId` TO `IX_users_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `tenants` RENAME INDEX `IX_Tenants_Identifier` TO `IX_tenants_Identifier`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `subscriptions` RENAME INDEX `IX_Subscriptions_TenantId` TO `IX_subscriptions_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `stockitems` RENAME INDEX `IX_StockItems_TenantId` TO `IX_stockitems_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `stockitems` RENAME INDEX `IX_StockItems_CompanyId_Name` TO `IX_stockitems_CompanyId_Name`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `signupcodes` RENAME INDEX `IX_SignupCodes_CodeHash` TO `IX_signupcodes_CodeHash`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` RENAME INDEX `IX_Sessions_TokenHash` TO `IX_sessions_TokenHash`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` RENAME INDEX `IX_Sessions_TenantId` TO `IX_sessions_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` RENAME INDEX `IX_Sessions_CompanyId` TO `IX_sessions_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` RENAME INDEX `IX_Sessions_AppUserId` TO `IX_sessions_AppUserId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `qrcodeaccesses` RENAME INDEX `IX_QrCodeAccesses_TenantId` TO `IX_qrcodeaccesses_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `qrcodeaccesses` RENAME INDEX `IX_QrCodeAccesses_PublicCode` TO `IX_qrcodeaccesses_PublicCode`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `qrcodeaccesses` RENAME INDEX `IX_QrCodeAccesses_CompanyId` TO `IX_qrcodeaccesses_CompanyId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `passwordresetrequests` RENAME INDEX `IX_PasswordResetRequests_TokenHash` TO `IX_passwordresetrequests_TokenHash`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `passwordresetrequests` RENAME INDEX `IX_PasswordResetRequests_AppUserId_IsActive` TO `IX_passwordresetrequests_AppUserId_IsActive`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `orderitems` RENAME INDEX `IX_OrderItems_CustomerOrderId` TO `IX_orderitems_CustomerOrderId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` RENAME INDEX `IX_MenuItems_TenantId` TO `IX_menuitems_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` RENAME INDEX `IX_MenuItems_MenuCategoryId` TO `IX_menuitems_MenuCategoryId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` RENAME INDEX `IX_MenuItems_CompanyId_MenuCategoryId_Name` TO `IX_menuitems_CompanyId_MenuCategoryId_Name`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menucategories` RENAME INDEX `IX_MenuCategories_TenantId` TO `IX_menucategories_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menucategories` RENAME INDEX `IX_MenuCategories_CompanyId_Name` TO `IX_menucategories_CompanyId_Name`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` RENAME INDEX `IX_DiningTables_TenantId` TO `IX_diningtables_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` RENAME INDEX `IX_DiningTables_QrCodeAccessId` TO `IX_diningtables_QrCodeAccessId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` RENAME INDEX `IX_DiningTables_CompanyId_InternalCode` TO `IX_diningtables_CompanyId_InternalCode`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` RENAME INDEX `IX_CustomerOrders_TenantId` TO `IX_customerorders_TenantId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` RENAME INDEX `IX_CustomerOrders_DiningTableId` TO `IX_customerorders_DiningTableId`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` RENAME INDEX `IX_CustomerOrders_CompanyId_Number` TO `IX_customerorders_CompanyId_Number`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `companies` RENAME INDEX `IX_Companies_TenantId_AccessSlug` TO `IX_companies_TenantId_AccessSlug`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `users` ADD CONSTRAINT `PK_users` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'users', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `tenants` ADD CONSTRAINT `PK_tenants` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'tenants', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `subscriptions` ADD CONSTRAINT `PK_subscriptions` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'subscriptions', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `stockitems` ADD CONSTRAINT `PK_stockitems` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'stockitems', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `signupcodes` ADD CONSTRAINT `PK_signupcodes` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'signupcodes', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` ADD CONSTRAINT `PK_sessions` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'sessions', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `qrcodeaccesses` ADD CONSTRAINT `PK_qrcodeaccesses` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'qrcodeaccesses', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `passwordresetrequests` ADD CONSTRAINT `PK_passwordresetrequests` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'passwordresetrequests', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `orderitems` ADD CONSTRAINT `PK_orderitems` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'orderitems', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` ADD CONSTRAINT `PK_menuitems` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'menuitems', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menucategories` ADD CONSTRAINT `PK_menucategories` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'menucategories', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` ADD CONSTRAINT `PK_diningtables` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'diningtables', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` ADD CONSTRAINT `PK_customerorders` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'customerorders', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `companies` ADD CONSTRAINT `PK_companies` PRIMARY KEY (`Id`);
    CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'companies', 'Id');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CREATE TABLE `waitercalls` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `DiningTableId` char(36) COLLATE ascii_general_ci NOT NULL,
        `RequestedAtUtc` datetime(6) NOT NULL,
        `ResolvedAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_waitercalls` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_waitercalls_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_waitercalls_diningtables_DiningTableId` FOREIGN KEY (`DiningTableId`) REFERENCES `diningtables` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_waitercalls_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CREATE INDEX `IX_waitercalls_CompanyId_DiningTableId_ResolvedAtUtc` ON `waitercalls` (`CompanyId`, `DiningTableId`, `ResolvedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CREATE INDEX `IX_waitercalls_DiningTableId` ON `waitercalls` (`DiningTableId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    CREATE INDEX `IX_waitercalls_TenantId` ON `waitercalls` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `companies` ADD CONSTRAINT `FK_companies_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` ADD CONSTRAINT `FK_customerorders_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` ADD CONSTRAINT `FK_customerorders_diningtables_DiningTableId` FOREIGN KEY (`DiningTableId`) REFERENCES `diningtables` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `customerorders` ADD CONSTRAINT `FK_customerorders_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` ADD CONSTRAINT `FK_diningtables_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` ADD CONSTRAINT `FK_diningtables_qrcodeaccesses_QrCodeAccessId` FOREIGN KEY (`QrCodeAccessId`) REFERENCES `qrcodeaccesses` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `diningtables` ADD CONSTRAINT `FK_diningtables_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menucategories` ADD CONSTRAINT `FK_menucategories_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menucategories` ADD CONSTRAINT `FK_menucategories_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` ADD CONSTRAINT `FK_menuitems_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` ADD CONSTRAINT `FK_menuitems_menucategories_MenuCategoryId` FOREIGN KEY (`MenuCategoryId`) REFERENCES `menucategories` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `menuitems` ADD CONSTRAINT `FK_menuitems_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `orderitems` ADD CONSTRAINT `FK_orderitems_customerorders_CustomerOrderId` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorders` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `passwordresetrequests` ADD CONSTRAINT `FK_passwordresetrequests_users_AppUserId` FOREIGN KEY (`AppUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `qrcodeaccesses` ADD CONSTRAINT `FK_qrcodeaccesses_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `qrcodeaccesses` ADD CONSTRAINT `FK_qrcodeaccesses_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` ADD CONSTRAINT `FK_sessions_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` ADD CONSTRAINT `FK_sessions_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `sessions` ADD CONSTRAINT `FK_sessions_users_AppUserId` FOREIGN KEY (`AppUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `stockitems` ADD CONSTRAINT `FK_stockitems_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `stockitems` ADD CONSTRAINT `FK_stockitems_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `subscriptions` ADD CONSTRAINT `FK_subscriptions_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `users` ADD CONSTRAINT `FK_users_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    ALTER TABLE `users` ADD CONSTRAINT `FK_users_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322054245_WaiterCallAlerts') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260322054245_WaiterCallAlerts', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322055405_OrderItemCategoryLabel') THEN

    ALTER TABLE `orderitems` ADD `CategoryName` varchar(120) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322055405_OrderItemCategoryLabel') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260322055405_OrderItemCategoryLabel', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322145005_AlertSettingsCrud') THEN

    ALTER TABLE `companies` ADD `AlertSoundUrl` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322145005_AlertSettingsCrud') THEN

    ALTER TABLE `companies` ADD `EnableOrderAlerts` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322145005_AlertSettingsCrud') THEN

    ALTER TABLE `companies` ADD `EnableWaiterCallAlerts` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322145005_AlertSettingsCrud') THEN

    UPDATE companies SET EnableOrderAlerts = 1, EnableWaiterCallAlerts = 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322145005_AlertSettingsCrud') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260322145005_AlertSettingsCrud', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322151939_AlertSoundTuning') THEN

    ALTER TABLE `companies` ADD `AlertPlaybackSeconds` int NOT NULL DEFAULT 6;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322151939_AlertSoundTuning') THEN

    ALTER TABLE `companies` ADD `AlertVolumePercent` int NOT NULL DEFAULT 100;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322151939_AlertSoundTuning') THEN

    UPDATE companies
    SET AlertPlaybackSeconds = 6,
        AlertVolumePercent = 100
    WHERE AlertPlaybackSeconds = 0
       OR AlertVolumePercent = 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322151939_AlertSoundTuning') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260322151939_AlertSoundTuning', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322153856_TableAlertSounds') THEN

    ALTER TABLE `diningtables` ADD `AlertSoundUrl` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322153856_TableAlertSounds') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260322153856_TableAlertSounds', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322220039_OrderItemImages') THEN

    ALTER TABLE `orderitems` ADD `ImageUrl` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322220039_OrderItemImages') THEN

    UPDATE orderitems oi
    INNER JOIN customerorders co ON co.Id = oi.CustomerOrderId
    INNER JOIN menuitems mi
        ON mi.CompanyId = co.CompanyId
        AND LOWER(TRIM(mi.Name)) = LOWER(TRIM(oi.Name))
    LEFT JOIN menucategories mc ON mc.Id = mi.MenuCategoryId
    SET oi.ImageUrl = mi.ImageUrl
    WHERE oi.ImageUrl IS NULL
      AND mi.ImageUrl IS NOT NULL
      AND (
          oi.CategoryName IS NULL
          OR LOWER(TRIM(oi.CategoryName)) = LOWER(TRIM(mc.Name))
      );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260322220039_OrderItemImages') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260322220039_OrderItemImages', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260323222223_OrderRequestedPaymentMethod') THEN

    ALTER TABLE `customerorders` ADD `RequestedPaymentMethod` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260323222223_OrderRequestedPaymentMethod') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260323222223_OrderRequestedPaymentMethod', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintAgentName` varchar(120) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintAttempts` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintClaimedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintLastError` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintPrinterName` varchar(180) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintQueuedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintStatus` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `customerorders` ADD `PrintedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `companies` ADD `EnableAutomaticPrinting` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `companies` ADD `PrintAgentKeyHash` varchar(128) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `companies` ADD `PrintAgentLastSeenAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `companies` ADD `PrintAgentName` varchar(120) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    ALTER TABLE `companies` ADD `PrintAgentPrinterName` varchar(180) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260326222057_PrintAutomationFlow') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260326222057_PrintAutomationFlow', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260327113949_DailyCashReportAudit') THEN

    CREATE TABLE `deletedorderrecords` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SourceOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrderNumber` int NOT NULL,
        `TableName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `CustomerName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `Notes` varchar(600) CHARACTER SET utf8mb4 NULL,
        `ItemsSummary` varchar(4000) CHARACTER SET utf8mb4 NOT NULL,
        `Status` int NOT NULL,
        `PaymentMethod` int NOT NULL,
        `RequestedPaymentMethod` int NOT NULL,
        `PaymentStatus` int NOT NULL,
        `PrintStatus` int NOT NULL,
        `TotalAmount` decimal(10,2) NOT NULL,
        `SubmittedAtUtc` datetime(6) NOT NULL,
        `PaidAtUtc` datetime(6) NULL,
        `PrintedAtUtc` datetime(6) NULL,
        `DeletedAtUtc` datetime(6) NOT NULL,
        `DeletedByUserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `DeletedByUserName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `DeletionReason` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_deletedorderrecords` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_deletedorderrecords_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_deletedorderrecords_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260327113949_DailyCashReportAudit') THEN

    CREATE INDEX `IX_deletedorderrecords_CompanyId_SubmittedAtUtc` ON `deletedorderrecords` (`CompanyId`, `SubmittedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260327113949_DailyCashReportAudit') THEN

    CREATE INDEX `IX_deletedorderrecords_SourceOrderId` ON `deletedorderrecords` (`SourceOrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260327113949_DailyCashReportAudit') THEN

    CREATE INDEX `IX_deletedorderrecords_TenantId` ON `deletedorderrecords` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260327113949_DailyCashReportAudit') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260327113949_DailyCashReportAudit', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260328034805_PrintPaperProfiles') THEN

    ALTER TABLE `companies` ADD `PrintOrdersPerPage` int NOT NULL DEFAULT 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260328034805_PrintPaperProfiles') THEN

    ALTER TABLE `companies` ADD `PrintPaperProfile` int NOT NULL DEFAULT 0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260328034805_PrintPaperProfiles') THEN

    UPDATE companies
    SET PrintOrdersPerPage = 1
    WHERE PrintOrdersPerPage IS NULL OR PrintOrdersPerPage < 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260328034805_PrintPaperProfiles') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260328034805_PrintPaperProfiles', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantFallbackMessage` varchar(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Agora preciso encaminhar voce para a equipe da unidade revisar isso com seguranca.';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantGreetingMessage` varchar(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Ola! Posso ajudar com duvidas do atendimento e encaminhar voce para o pedido oficial da unidade.';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantMaxOutputTokens` int NOT NULL DEFAULT 220;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantModel` varchar(80) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'gpt-5.4-mini';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantRedirectMessage` varchar(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Para finalizar com seguranca, use o link oficial do ZeroPaper da unidade e conclua os dados no sistema.';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantSystemPrompt` varchar(4000) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Atue como atendente digital do ZeroPaper em portugues do Brasil. Seja objetivo, cordial e claro. Nao invente itens, valores, disponibilidade ou prazos. Oriente o cliente para o fluxo oficial do sistema sempre que for necessario fechar pedido, confirmar endereco, nome ou pagamento. Se houver duvida fora desse escopo, admita o limite e use a mensagem de fallback configurada pela unidade.';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    ALTER TABLE `companies` ADD `EnableAiAssistant` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402111410_AiAssistantSettings') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402111410_AiAssistantSettings', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402121354_AiAssistantOrderingLink') THEN

    ALTER TABLE `companies` ADD `AiAssistantOrderingLink` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402121354_AiAssistantOrderingLink') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402121354_AiAssistantOrderingLink', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402123242_DeliveryChannelTable') THEN

    ALTER TABLE `diningtables` ADD `IsDeliveryChannel` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402123242_DeliveryChannelTable') THEN

    CREATE INDEX `IX_diningtables_CompanyId_IsDeliveryChannel` ON `diningtables` (`CompanyId`, `IsDeliveryChannel`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402123242_DeliveryChannelTable') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402123242_DeliveryChannelTable', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402125330_DeliveryOrderFields') THEN

    ALTER TABLE `customerorders` ADD `DeliveryAddress` varchar(220) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402125330_DeliveryOrderFields') THEN

    ALTER TABLE `customerorders` ADD `DeliveryComplement` varchar(160) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402125330_DeliveryOrderFields') THEN

    ALTER TABLE `customerorders` ADD `DeliveryNumber` varchar(30) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402125330_DeliveryOrderFields') THEN

    ALTER TABLE `customerorders` ADD `DeliveryPhone` varchar(40) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402125330_DeliveryOrderFields') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402125330_DeliveryOrderFields', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402132341_DeliveryEditWindow') THEN

    ALTER TABLE `orderitems` ADD `SourceMenuItemId` char(36) COLLATE ascii_general_ci NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402132341_DeliveryEditWindow') THEN

    ALTER TABLE `customerorders` ADD `PublicEditAllowedUntilUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402132341_DeliveryEditWindow') THEN

    ALTER TABLE `customerorders` ADD `PublicEditCode` varchar(64) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402132341_DeliveryEditWindow') THEN

    CREATE UNIQUE INDEX `IX_customerorders_PublicEditCode` ON `customerorders` (`PublicEditCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402132341_DeliveryEditWindow') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402132341_DeliveryEditWindow', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402212009_AdminControlCenter') THEN

    ALTER TABLE `companies` ADD `AdminMasterPasswordCipherText` varchar(1000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402212009_AdminControlCenter') THEN

    ALTER TABLE `companies` ADD `AdminMasterPasswordHash` varchar(255) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402212009_AdminControlCenter') THEN

    ALTER TABLE `companies` ADD `AdminMasterPasswordRotatedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402212009_AdminControlCenter') THEN

    CREATE TABLE `aiassistantinteractions` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Source` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `Model` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
        `Succeeded` tinyint(1) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_aiassistantinteractions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_aiassistantinteractions_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402212009_AdminControlCenter') THEN

    CREATE INDEX `IX_aiassistantinteractions_CompanyId_CreatedAtUtc` ON `aiassistantinteractions` (`CompanyId`, `CreatedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402212009_AdminControlCenter') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402212009_AdminControlCenter', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    ALTER TABLE `orderitems` ADD `BaseUnitPrice` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE TABLE `menuitemadditionalgroups` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `MenuItemId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `AllowMultiple` tinyint(1) NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_menuitemadditionalgroups` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_menuitemadditionalgroups_menuitems_MenuItemId` FOREIGN KEY (`MenuItemId`) REFERENCES `menuitems` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE TABLE `orderitemadditionalselections` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrderItemId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SourceMenuItemAdditionalOptionId` char(36) COLLATE ascii_general_ci NULL,
        `GroupName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `OptionName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `UnitPrice` decimal(10,2) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_orderitemadditionalselections` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_orderitemadditionalselections_orderitems_OrderItemId` FOREIGN KEY (`OrderItemId`) REFERENCES `orderitems` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE TABLE `menuitemadditionaloptions` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `MenuItemId` char(36) COLLATE ascii_general_ci NOT NULL,
        `MenuItemAdditionalGroupId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Price` decimal(10,2) NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_menuitemadditionaloptions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_menuitemadditionaloptions_menuitemadditionalgroups_MenuItemA~` FOREIGN KEY (`MenuItemAdditionalGroupId`) REFERENCES `menuitemadditionalgroups` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_menuitemadditionaloptions_menuitems_MenuItemId` FOREIGN KEY (`MenuItemId`) REFERENCES `menuitems` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE UNIQUE INDEX `IX_menuitemadditionalgroups_MenuItemId_Name` ON `menuitemadditionalgroups` (`MenuItemId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE UNIQUE INDEX `IX_menuitemadditionaloptions_MenuItemAdditionalGroupId_Name` ON `menuitemadditionaloptions` (`MenuItemAdditionalGroupId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE INDEX `IX_menuitemadditionaloptions_MenuItemId` ON `menuitemadditionaloptions` (`MenuItemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    CREATE INDEX `IX_orderitemadditionalselections_OrderItemId` ON `orderitemadditionalselections` (`OrderItemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402223111_MenuItemAdditionalsAndOrderSelections') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402223111_MenuItemAdditionalsAndOrderSelections', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesAiAssistantModule` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesCashModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesDeliveryModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesKitchenModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesMenuModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesPrintingModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesStockModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesTablesModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    ALTER TABLE `subscriptions` ADD `IncludesWaiterCallModule` tinyint(1) NOT NULL DEFAULT TRUE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    UPDATE subscriptions
    SET IncludesMenuModule = 1,
        IncludesTablesModule = 1,
        IncludesKitchenModule = 1,
        IncludesCashModule = 1,
        IncludesStockModule = 1,
        IncludesDeliveryModule = 1,
        IncludesPrintingModule = 1,
        IncludesWaiterCallModule = 1
    WHERE IsActive = 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    UPDATE subscriptions
    SET MonthlyPrice = 79.90,
        PlanName = 'ZeroPaper Operacao'
    WHERE IsActive = 1
      AND (MonthlyPrice = 0 OR PlanName = 'ZeroPaper Base' OR PlanName = 'Plano nao informado');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    UPDATE subscriptions s
    INNER JOIN companies c ON c.TenantId = s.TenantId
    SET s.IncludesAiAssistantModule = 1,
        s.MonthlyPrice = 119.90,
        s.PlanName = 'ZeroPaper Operacao + IA'
    WHERE s.IsActive = 1
      AND c.EnableAiAssistant = 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260402224750_SubscriptionFeatureModules') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260402224750_SubscriptionFeatureModules', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `EnableWhatsAppAssistant` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `IsWhatsAppConnected` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppAccountSecurityTokenCipherText` varchar(2000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppConnectedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppConnectedPhone` varchar(40) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppDisconnectedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppInstanceId` varchar(80) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppInstanceTokenCipherText` varchar(2000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppLastIncomingAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppLastOutgoingAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    ALTER TABLE `companies` ADD `WhatsAppWebhookSecretCipherText` varchar(2000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE TABLE `whatsappconversations` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ExternalPhone` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `CustomerName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `LastMessagePreview` varchar(280) CHARACTER SET utf8mb4 NULL,
        `LastDirection` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `LastIncomingAtUtc` datetime(6) NULL,
        `LastOutgoingAtUtc` datetime(6) NULL,
        `LastInteractionAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_whatsappconversations` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_whatsappconversations_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE TABLE `whatsappmessages` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `WhatsAppConversationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `IsInbound` tinyint(1) NOT NULL,
        `MessageType` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `Content` varchar(4000) CHARACTER SET utf8mb4 NOT NULL,
        `ExternalMessageId` varchar(180) CHARACTER SET utf8mb4 NULL,
        `Status` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `GeneratedByAi` tinyint(1) NOT NULL,
        `DeliveredAtUtc` datetime(6) NULL,
        `ReadAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_whatsappmessages` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_whatsappmessages_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_whatsappmessages_whatsappconversations_WhatsAppConversationId` FOREIGN KEY (`WhatsAppConversationId`) REFERENCES `whatsappconversations` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE UNIQUE INDEX `IX_whatsappconversations_CompanyId_ExternalPhone` ON `whatsappconversations` (`CompanyId`, `ExternalPhone`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE INDEX `IX_whatsappconversations_CompanyId_LastInteractionAtUtc` ON `whatsappconversations` (`CompanyId`, `LastInteractionAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE INDEX `IX_whatsappmessages_CompanyId_CreatedAtUtc` ON `whatsappmessages` (`CompanyId`, `CreatedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE INDEX `IX_whatsappmessages_ExternalMessageId` ON `whatsappmessages` (`ExternalMessageId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    CREATE INDEX `IX_whatsappmessages_WhatsAppConversationId` ON `whatsappmessages` (`WhatsAppConversationId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260403040754_WhatsAppAssistantIntegration') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260403040754_WhatsAppAssistantIntegration', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404053321_AiAssistantServiceHours') THEN

    ALTER TABLE `companies` ADD `AiAssistantServiceEndTime` varchar(5) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404053321_AiAssistantServiceHours') THEN

    ALTER TABLE `companies` ADD `AiAssistantServiceStartTime` varchar(5) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404053321_AiAssistantServiceHours') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260404053321_AiAssistantServiceHours', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404093034_MenuAdditionalCatalog') THEN

    CREATE TABLE `menuadditionalcataloggroups` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `AllowMultiple` tinyint(1) NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_menuadditionalcataloggroups` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404093034_MenuAdditionalCatalog') THEN

    CREATE TABLE `menuadditionalcatalogoptions` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `MenuAdditionalCatalogGroupId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Price` decimal(10,2) NOT NULL,
        `DisplayOrder` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_menuadditionalcatalogoptions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_menuadditionalcatalogoptions_menuadditionalcataloggroups_Men~` FOREIGN KEY (`MenuAdditionalCatalogGroupId`) REFERENCES `menuadditionalcataloggroups` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404093034_MenuAdditionalCatalog') THEN

    CREATE UNIQUE INDEX `IX_menuadditionalcataloggroups_CompanyId_Name` ON `menuadditionalcataloggroups` (`CompanyId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404093034_MenuAdditionalCatalog') THEN

    CREATE UNIQUE INDEX `IX_menuadditionalcatalogoptions_MenuAdditionalCatalogGroupId_Na~` ON `menuadditionalcatalogoptions` (`MenuAdditionalCatalogGroupId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260404093034_MenuAdditionalCatalog') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260404093034_MenuAdditionalCatalog', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `customerorders` ADD `DeliveryDistanceKm` decimal(10,2) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `customerorders` ADD `DeliveryFreightAmount` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `customerorders` ADD `DeliveryFreightCalculatedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `customerorders` ADD `DeliveryFreightProvider` varchar(40) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `customerorders` ADD `DeliveryPostalCode` varchar(8) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `companies` ADD `DeliveryFreightBaseFee` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `companies` ADD `DeliveryFreightPricePerKm` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `companies` ADD `DeliveryOriginPostalCode` varchar(8) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    ALTER TABLE `companies` ADD `EnableDeliveryFreight` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    CREATE TABLE `deliverydistancecaches` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OriginPostalCode` varchar(8) CHARACTER SET utf8mb4 NOT NULL,
        `DestinationPostalCode` varchar(8) CHARACTER SET utf8mb4 NOT NULL,
        `Provider` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `DistanceKm` decimal(10,2) NOT NULL,
        `ExpiresAtUtc` datetime(6) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_deliverydistancecaches` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_deliverydistancecaches_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    CREATE UNIQUE INDEX `IX_deliverydistancecaches_CompanyId_Provider_OriginPostalCode_D~` ON `deliverydistancecaches` (`CompanyId`, `Provider`, `OriginPostalCode`, `DestinationPostalCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    CREATE INDEX `IX_deliverydistancecaches_ExpiresAtUtc` ON `deliverydistancecaches` (`ExpiresAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260408084204_DeliveryFreightSettings') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260408084204_DeliveryFreightSettings', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260409222548_AiAssistantPixSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantPixKey` varchar(180) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260409222548_AiAssistantPixSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantPixMessage` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260409222548_AiAssistantPixSettings') THEN

    ALTER TABLE `companies` ADD `AiAssistantPixReceiverName` varchar(120) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260409222548_AiAssistantPixSettings') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260409222548_AiAssistantPixSettings', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260415105359_DiningTableComandaLabel') THEN

    ALTER TABLE `diningtables` ADD `ComandaLabel` varchar(40) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260415105359_DiningTableComandaLabel') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260415105359_DiningTableComandaLabel', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260503233106_DeliveryFreightMinimumDistance') THEN

    ALTER TABLE `companies` ADD `DeliveryFreightBaseDistanceKm` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260503233106_DeliveryFreightMinimumDistance') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260503233106_DeliveryFreightMinimumDistance', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    ALTER TABLE `users` ADD `ShortcutAccessCreatedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    ALTER TABLE `users` ADD `ShortcutAccessExpiresAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    ALTER TABLE `users` ADD `ShortcutAccessLastUsedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    ALTER TABLE `users` ADD `ShortcutAccessRevokedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    ALTER TABLE `users` ADD `ShortcutAccessTokenHash` varchar(128) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    CREATE UNIQUE INDEX `IX_users_ShortcutAccessTokenHash` ON `users` (`ShortcutAccessTokenHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504062735_OwnerShortcutAccess') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260504062735_OwnerShortcutAccess', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504142246_DeliveryCustomerProfiles') THEN

    CREATE TABLE `deliverycustomerprofiles` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Phone` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `CustomerName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `DeliveryAddress` varchar(220) CHARACTER SET utf8mb4 NULL,
        `DeliveryNumber` varchar(30) CHARACTER SET utf8mb4 NULL,
        `DeliveryComplement` varchar(160) CHARACTER SET utf8mb4 NULL,
        `DeliveryPostalCode` varchar(8) CHARACTER SET utf8mb4 NULL,
        `LastOrderAtUtc` datetime(6) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_deliverycustomerprofiles` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_deliverycustomerprofiles_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_deliverycustomerprofiles_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504142246_DeliveryCustomerProfiles') THEN

    CREATE INDEX `IX_deliverycustomerprofiles_CompanyId_LastOrderAtUtc` ON `deliverycustomerprofiles` (`CompanyId`, `LastOrderAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504142246_DeliveryCustomerProfiles') THEN

    CREATE UNIQUE INDEX `IX_deliverycustomerprofiles_CompanyId_Phone` ON `deliverycustomerprofiles` (`CompanyId`, `Phone`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504142246_DeliveryCustomerProfiles') THEN

    CREATE INDEX `IX_deliverycustomerprofiles_TenantId` ON `deliverycustomerprofiles` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504142246_DeliveryCustomerProfiles') THEN

    INSERT IGNORE INTO `deliverycustomerprofiles`
        (`Id`, `TenantId`, `CompanyId`, `Phone`, `CustomerName`, `DeliveryAddress`, `DeliveryNumber`, `DeliveryComplement`, `DeliveryPostalCode`, `LastOrderAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`, `IsActive`)
    SELECT
        UUID(),
        ranked.`TenantId`,
        ranked.`CompanyId`,
        ranked.`PhoneNormalized`,
        ranked.`CustomerName`,
        ranked.`DeliveryAddress`,
        ranked.`DeliveryNumber`,
        ranked.`DeliveryComplement`,
        ranked.`DeliveryPostalCode`,
        ranked.`SubmittedAtUtc`,
        UTC_TIMESTAMP(6),
        UTC_TIMESTAMP(6),
        TRUE
    FROM (
        SELECT
            normalized.*,
            ROW_NUMBER() OVER (
                PARTITION BY normalized.`CompanyId`, normalized.`PhoneNormalized`
                ORDER BY normalized.`SubmittedAtUtc` DESC, normalized.`Id` DESC
            ) AS `RowNumber`
        FROM (
            SELECT
                clean.`Id`,
                clean.`TenantId`,
                clean.`CompanyId`,
                CASE
                    WHEN CHAR_LENGTH(clean.`PhoneDigits`) IN (10, 11) THEN CONCAT('55', clean.`PhoneDigits`)
                    ELSE clean.`PhoneDigits`
                END AS `PhoneNormalized`,
                clean.`CustomerName`,
                clean.`DeliveryAddress`,
                clean.`DeliveryNumber`,
                clean.`DeliveryComplement`,
                clean.`DeliveryPostalCode`,
                clean.`SubmittedAtUtc`
            FROM (
                SELECT
                    orders.`Id`,
                    orders.`TenantId`,
                    orders.`CompanyId`,
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(TRIM(orders.`DeliveryPhone`), ' ', ''), '-', ''), '(', ''), ')', ''), '+', ''), '.', '') AS `PhoneDigits`,
                    orders.`CustomerName`,
                    orders.`DeliveryAddress`,
                    orders.`DeliveryNumber`,
                    orders.`DeliveryComplement`,
                    orders.`DeliveryPostalCode`,
                    orders.`SubmittedAtUtc`
                FROM `customerorders` orders
                INNER JOIN `diningtables` tables ON tables.`Id` = orders.`DiningTableId`
                WHERE orders.`IsActive` = TRUE
                  AND orders.`DeliveryPhone` IS NOT NULL
                  AND tables.`IsDeliveryChannel` = TRUE
            ) clean
        ) normalized
        WHERE normalized.`PhoneNormalized` <> ''
    ) ranked
    WHERE ranked.`RowNumber` = 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504142246_DeliveryCustomerProfiles') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260504142246_DeliveryCustomerProfiles', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504154316_DeliveryCustomerShortLinks') THEN

    ALTER TABLE `deliverycustomerprofiles` ADD `PublicAccessCodeCipherText` varchar(1000) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504154316_DeliveryCustomerShortLinks') THEN

    ALTER TABLE `deliverycustomerprofiles` ADD `PublicAccessCodeCreatedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504154316_DeliveryCustomerShortLinks') THEN

    ALTER TABLE `deliverycustomerprofiles` ADD `PublicAccessCodeHash` varchar(128) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504154316_DeliveryCustomerShortLinks') THEN

    CREATE UNIQUE INDEX `IX_deliverycustomerprofiles_PublicAccessCodeHash` ON `deliverycustomerprofiles` (`PublicAccessCodeHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504154316_DeliveryCustomerShortLinks') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260504154316_DeliveryCustomerShortLinks', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504213758_AiAssistantServiceDays') THEN

    ALTER TABLE `companies` ADD `AiAssistantServiceDays` varchar(20) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260504213758_AiAssistantServiceDays') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260504213758_AiAssistantServiceDays', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506102519_MenuItemMaxAdditionalSelections') THEN

    ALTER TABLE `menuitems` ADD `MaxAdditionalSelections` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506102519_MenuItemMaxAdditionalSelections') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260506102519_MenuItemMaxAdditionalSelections', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506221220_MenuItemAdditionalGroupMaxSelections') THEN

    ALTER TABLE `menuitemadditionalgroups` ADD `MaxAdditionalSelections` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506221220_MenuItemAdditionalGroupMaxSelections') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260506221220_MenuItemAdditionalGroupMaxSelections', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    ALTER TABLE `menuitemadditionaloptions` ADD `SourceMenuAdditionalCatalogOptionId` char(36) COLLATE ascii_general_ci NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    ALTER TABLE `menuitemadditionalgroups` ADD `SourceMenuAdditionalCatalogGroupId` char(36) COLLATE ascii_general_ci NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    ALTER TABLE `menuadditionalcataloggroups` ADD `MaxAdditionalSelections` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    UPDATE menuadditionalcataloggroups mcg
    JOIN menuadditionalcatalogoptions mco
        ON mco.MenuAdditionalCatalogGroupId = mcg.Id
        AND mco.CompanyId = mcg.CompanyId
        AND mco.IsActive = 1
    JOIN menuitemadditionaloptions mio
        ON mio.CompanyId = mcg.CompanyId
        AND mio.Name = mco.Name
        AND mio.Price = mco.Price
        AND mio.IsActive = 1
    JOIN menuitemadditionalgroups mig
        ON mig.Id = mio.MenuItemAdditionalGroupId
        AND mig.CompanyId = mcg.CompanyId
        AND mig.IsActive = 1
    SET mcg.MaxAdditionalSelections = mig.MaxAdditionalSelections
    WHERE mcg.MaxAdditionalSelections IS NULL
        AND mig.MaxAdditionalSelections IS NOT NULL
        AND (
            LOWER(mig.Name) = LOWER(mcg.Name)
            OR LOWER(mig.Name) = LOWER(mco.Name)
        );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    UPDATE menuitemadditionalgroups mig
    JOIN menuitemadditionaloptions mio
        ON mio.MenuItemAdditionalGroupId = mig.Id
        AND mio.CompanyId = mig.CompanyId
        AND mio.IsActive = 1
    JOIN menuadditionalcatalogoptions mco
        ON mco.CompanyId = mig.CompanyId
        AND mco.Name = mio.Name
        AND mco.Price = mio.Price
        AND mco.IsActive = 1
    JOIN menuadditionalcataloggroups mcg
        ON mcg.Id = mco.MenuAdditionalCatalogGroupId
        AND mcg.CompanyId = mig.CompanyId
        AND mcg.IsActive = 1
    SET mig.SourceMenuAdditionalCatalogGroupId = mcg.Id,
        mig.MaxAdditionalSelections = COALESCE(mig.MaxAdditionalSelections, mcg.MaxAdditionalSelections),
        mio.SourceMenuAdditionalCatalogOptionId = mco.Id
    WHERE mig.SourceMenuAdditionalCatalogGroupId IS NULL
        AND (
            LOWER(mig.Name) = LOWER(mcg.Name)
            OR LOWER(mig.Name) = LOWER(mco.Name)
        );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    CREATE INDEX `IX_menuitemadditionaloptions_SourceMenuAdditionalCatalogOptionId` ON `menuitemadditionaloptions` (`SourceMenuAdditionalCatalogOptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    CREATE INDEX `IX_menuitemadditionalgroups_SourceMenuAdditionalCatalogGroupId` ON `menuitemadditionalgroups` (`SourceMenuAdditionalCatalogGroupId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260506223348_MenuCatalogAdditionalsAsSource') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260506223348_MenuCatalogAdditionalsAsSource', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260507011809_CompanyLogoUrl') THEN

    ALTER TABLE `companies` ADD `LogoUrl` text CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260507011809_CompanyLogoUrl') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260507011809_CompanyLogoUrl', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602033030_ManualPixConfirmations') THEN

    CREATE TABLE `manualpixconfirmations` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrderId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `CustomerPhone` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `Amount` decimal(10,2) NOT NULL,
        `PixKeyShown` varchar(180) CHARACTER SET utf8mb4 NOT NULL,
        `ConfirmationPhrase` varchar(180) CHARACTER SET utf8mb4 NOT NULL,
        `CustomerMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `ReceiptReference` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Status` int NOT NULL,
        `CustomerConfirmedAtUtc` datetime(6) NULL,
        `ReviewedAtUtc` datetime(6) NULL,
        `ReviewedByUserId` char(36) COLLATE ascii_general_ci NULL,
        `OwnerNote` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_manualpixconfirmations` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_manualpixconfirmations_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_manualpixconfirmations_customerorders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `customerorders` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_manualpixconfirmations_users_ReviewedByUserId` FOREIGN KEY (`ReviewedByUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602033030_ManualPixConfirmations') THEN

    CREATE INDEX `IX_manualpixconfirmations_CompanyId_CustomerPhone_CreatedAtUtc` ON `manualpixconfirmations` (`CompanyId`, `CustomerPhone`, `CreatedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602033030_ManualPixConfirmations') THEN

    CREATE INDEX `IX_manualpixconfirmations_CompanyId_Status_CustomerConfirmedAtU~` ON `manualpixconfirmations` (`CompanyId`, `Status`, `CustomerConfirmedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602033030_ManualPixConfirmations') THEN

    CREATE INDEX `IX_manualpixconfirmations_OrderId` ON `manualpixconfirmations` (`OrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602033030_ManualPixConfirmations') THEN

    CREATE INDEX `IX_manualpixconfirmations_ReviewedByUserId` ON `manualpixconfirmations` (`ReviewedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602033030_ManualPixConfirmations') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260602033030_ManualPixConfirmations', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602034325_MenuCategoryImageFallback') THEN

    ALTER TABLE `menucategories` ADD `ImageUrl` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260602034325_MenuCategoryImageFallback') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260602034325_MenuCategoryImageFallback', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603224154_DeliveryEstimatedWaitTimes') THEN

    ALTER TABLE `companies` ADD `DeliveryEstimatedMinutes` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603224154_DeliveryEstimatedWaitTimes') THEN

    ALTER TABLE `companies` ADD `PickupEstimatedMinutes` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603224154_DeliveryEstimatedWaitTimes') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260603224154_DeliveryEstimatedWaitTimes', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    ALTER TABLE `customerorders` ADD `DiscountAmount` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    ALTER TABLE `customerorders` ADD `EditedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    ALTER TABLE `customerorders` ADD `IsEdited` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    ALTER TABLE `customerorders` ADD `PriceAdjustedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    ALTER TABLE `customerorders` ADD `PriceAdjustmentNote` varchar(240) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    ALTER TABLE `customerorders` ADD `SurchargeAmount` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260603234211_OrderEditingAndPriceAdjustment') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260603234211_OrderEditingAndPriceAdjustment', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260604034952_CustomerOrderPaymentSplits') THEN

    CREATE TABLE `customerorderpayments` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerOrderId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Method` int NOT NULL,
        `Amount` decimal(10,2) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_customerorderpayments` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_customerorderpayments_customerorders_CustomerOrderId` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorders` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260604034952_CustomerOrderPaymentSplits') THEN

    CREATE INDEX `IX_customerorderpayments_CustomerOrderId` ON `customerorderpayments` (`CustomerOrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260604034952_CustomerOrderPaymentSplits') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260604034952_CustomerOrderPaymentSplits', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260616114931_CompanyTimeZoneId') THEN

    ALTER TABLE `companies` ADD `TimeZoneId` varchar(100) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'America/Sao_Paulo';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260616114931_CompanyTimeZoneId') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260616114931_CompanyTimeZoneId', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260616121858_CurrentProjectModelSync') THEN

    CREATE TABLE `dailysalessnapshots` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ReferenceDate` date NOT NULL,
        `OrdersSubmittedCount` int NOT NULL,
        `PaidOrdersCount` int NOT NULL,
        `PendingOrdersCount` int NOT NULL,
        `CancelledOrdersCount` int NOT NULL,
        `TotalSalesAmount` decimal(12,2) NOT NULL,
        `PaidAmount` decimal(12,2) NOT NULL,
        `PendingAmount` decimal(12,2) NOT NULL,
        `CancelledAmount` decimal(12,2) NOT NULL,
        `DiscountAmount` decimal(12,2) NOT NULL,
        `SurchargeAmount` decimal(12,2) NOT NULL,
        `DeliveryFreightAmount` decimal(12,2) NOT NULL,
        `AverageTicket` decimal(12,2) NOT NULL,
        `HasDetailedData` tinyint(1) NOT NULL,
        `DetailExpiresAtUtc` datetime(6) NOT NULL,
        `DetailPurgedAtUtc` datetime(6) NULL,
        `GeneratedAtUtc` datetime(6) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_dailysalessnapshots` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_dailysalessnapshots_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260616121858_CurrentProjectModelSync') THEN

    CREATE UNIQUE INDEX `IX_dailysalessnapshots_CompanyId_ReferenceDate` ON `dailysalessnapshots` (`CompanyId`, `ReferenceDate`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260616121858_CurrentProjectModelSync') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260616121858_CurrentProjectModelSync', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260618035247_SalesReportDailyOrderIndex') THEN

    CREATE INDEX `IX_customerorders_CompanyId_SubmittedAtUtc_Status_PaymentStatus` ON `customerorders` (`CompanyId`, `SubmittedAtUtc`, `Status`, `PaymentStatus`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260618035247_SalesReportDailyOrderIndex') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260618035247_SalesReportDailyOrderIndex', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE TABLE `printagents` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `TokenHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NULL,
        `PrinterName` varchar(180) CHARACTER SET utf8mb4 NULL,
        `AppVersion` varchar(60) CHARACTER SET utf8mb4 NULL,
        `RegisteredAtUtc` datetime(6) NULL,
        `LastSeenAtUtc` datetime(6) NULL,
        `TokenRotatedAtUtc` datetime(6) NOT NULL,
        `LastError` varchar(500) CHARACTER SET utf8mb4 NULL,
        `LastErrorAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_printagents` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_printagents_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_printagents_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE TABLE `printjobs` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SourceOrderId` char(36) COLLATE ascii_general_ci NULL,
        `Kind` int NOT NULL,
        `Status` int NOT NULL,
        `Title` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `Notes` varchar(600) CHARACTER SET utf8mb4 NULL,
        `AgentName` varchar(120) CHARACTER SET utf8mb4 NULL,
        `PrinterName` varchar(180) CHARACTER SET utf8mb4 NULL,
        `LastError` varchar(500) CHARACTER SET utf8mb4 NULL,
        `QueuedAtUtc` datetime(6) NOT NULL,
        `ClaimedAtUtc` datetime(6) NULL,
        `PrintedAtUtc` datetime(6) NULL,
        `Attempts` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_printjobs` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_printjobs_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_printjobs_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_customerorders_CompanyId_PrintStatus_SubmittedAtUtc` ON `customerorders` (`CompanyId`, `PrintStatus`, `SubmittedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_printagents_CompanyId_IsActive` ON `printagents` (`CompanyId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_printagents_CompanyId_LastSeenAtUtc` ON `printagents` (`CompanyId`, `LastSeenAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_printagents_TenantId` ON `printagents` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE UNIQUE INDEX `IX_printagents_TokenHash` ON `printagents` (`TokenHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_printjobs_CompanyId_SourceOrderId` ON `printjobs` (`CompanyId`, `SourceOrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_printjobs_CompanyId_Status_QueuedAtUtc` ON `printjobs` (`CompanyId`, `Status`, `QueuedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    CREATE INDEX `IX_printjobs_TenantId` ON `printjobs` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260621224151_PrintAgentProfessionalFlow') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260621224151_PrintAgentProfessionalFlow', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    ALTER TABLE `customerorders` ADD `CouponAppliedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    ALTER TABLE `customerorders` ADD `CouponCode` varchar(40) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    ALTER TABLE `customerorders` ADD `CouponDiscountAmount` decimal(10,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    ALTER TABLE `customerorders` ADD `CouponId` char(36) COLLATE ascii_general_ci NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    CREATE TABLE `coupons` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Code` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(240) CHARACTER SET utf8mb4 NULL,
        `DiscountType` int NOT NULL,
        `DiscountValue` decimal(10,2) NOT NULL,
        `MinimumOrderAmount` decimal(10,2) NOT NULL,
        `StartsAtUtc` datetime(6) NULL,
        `EndsAtUtc` datetime(6) NULL,
        `UsageLimit` int NULL,
        `UsageCount` int NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_coupons` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_coupons_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_coupons_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    CREATE INDEX `IX_customerorders_CouponId` ON `customerorders` (`CouponId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    CREATE UNIQUE INDEX `IX_coupons_CompanyId_Code` ON `coupons` (`CompanyId`, `Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    CREATE INDEX `IX_coupons_CompanyId_IsActive` ON `coupons` (`CompanyId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    CREATE INDEX `IX_coupons_TenantId` ON `coupons` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    ALTER TABLE `customerorders` ADD CONSTRAINT `FK_customerorders_coupons_CouponId` FOREIGN KEY (`CouponId`) REFERENCES `coupons` (`Id`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623034144_CouponsAndCashClosing') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260623034144_CouponsAndCashClosing', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `IsMercadoPagoConnected` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoAccessTokenCipherText` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoConnectedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoDisconnectedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoLiveMode` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoPublicKey` varchar(200) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoRefreshTokenCipherText` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoTokenExpiresAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    ALTER TABLE `companies` ADD `MercadoPagoUserId` varchar(40) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623083319_MercadoPagoConnect') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260623083319_MercadoPagoConnect', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    ALTER TABLE `deliverycustomerprofiles` ADD `DeliveryNeighborhood` varchar(120) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE TABLE `customerorderhistories` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerProfileId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrderId` char(36) COLLATE ascii_general_ci NOT NULL,
        `TotalAmount` decimal(10,2) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_customerorderhistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_customerorderhistories_deliverycustomerprofiles_CustomerProf~` FOREIGN KEY (`CustomerProfileId`) REFERENCES `deliverycustomerprofiles` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_customerorderhistories_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE TABLE `customerorderhistoryitems` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerOrderHistoryId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ItemName` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `Quantity` decimal(10,2) NOT NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_customerorderhistoryitems` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_customerorderhistoryitems_customerorderhistories_CustomerOrd~` FOREIGN KEY (`CustomerOrderHistoryId`) REFERENCES `customerorderhistories` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_customerorderhistoryitems_tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `tenants` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE INDEX `IX_customerorderhistories_CompanyId_CustomerProfileId_CreatedAt~` ON `customerorderhistories` (`CompanyId`, `CustomerProfileId`, `CreatedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE INDEX `IX_customerorderhistories_CustomerProfileId` ON `customerorderhistories` (`CustomerProfileId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE UNIQUE INDEX `IX_customerorderhistories_OrderId` ON `customerorderhistories` (`OrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE INDEX `IX_customerorderhistories_TenantId` ON `customerorderhistories` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE INDEX `IX_customerorderhistoryitems_CustomerOrderHistoryId` ON `customerorderhistoryitems` (`CustomerOrderHistoryId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    CREATE INDEX `IX_customerorderhistoryitems_TenantId` ON `customerorderhistoryitems` (`TenantId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260623221901_CustomerProfileOrderHistory') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260623221901_CustomerProfileOrderHistory', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    ALTER TABLE `customerorders` ADD `SalesAgentId` char(36) COLLATE ascii_general_ci NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    ALTER TABLE `customerorders` ADD `SalesOrigin` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    CREATE TABLE `salesagents` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Phone` varchar(30) CHARACTER SET utf8mb4 NULL,
        `Code` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `CommissionPercent` decimal(5,2) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_salesagents` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_salesagents_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    CREATE INDEX `IX_customerorders_SalesAgentId` ON `customerorders` (`SalesAgentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    CREATE UNIQUE INDEX `IX_salesagents_Code` ON `salesagents` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    CREATE INDEX `IX_salesagents_CompanyId_IsActive` ON `salesagents` (`CompanyId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    ALTER TABLE `customerorders` ADD CONSTRAINT `FK_customerorders_salesagents_SalesAgentId` FOREIGN KEY (`SalesAgentId`) REFERENCES `salesagents` (`Id`) ON DELETE SET NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260630043611_AddSalesAgents') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260630043611_AddSalesAgents', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    ALTER TABLE `menuitems` ADD `EstimatedDurationMinutes` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    ALTER TABLE `menuitems` ADD `Kind` int NOT NULL DEFAULT 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    ALTER TABLE `companies` ADD `BusinessSegment` int NOT NULL DEFAULT 1;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE TABLE `pets` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerProfileId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Species` int NOT NULL,
        `Size` int NOT NULL,
        `Breed` varchar(120) CHARACTER SET utf8mb4 NULL,
        `WeightKg` decimal(7,2) NULL,
        `BirthDate` date NULL,
        `BehaviorNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `AllergyNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Restrictions` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `PhotoUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_pets` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_pets_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_pets_deliverycustomerprofiles_CustomerProfileId` FOREIGN KEY (`CustomerProfileId`) REFERENCES `deliverycustomerprofiles` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE TABLE `appointments` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `PetId` char(36) COLLATE ascii_general_ci NOT NULL,
        `MenuItemId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CustomerOrderId` char(36) COLLATE ascii_general_ci NULL,
        `AssignedUserId` char(36) COLLATE ascii_general_ci NULL,
        `StartsAtUtc` datetime(6) NOT NULL,
        `DurationMinutes` int NOT NULL,
        `Status` int NOT NULL,
        `ServiceNameSnapshot` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
        `UnitPriceSnapshot` decimal(10,2) NOT NULL,
        `CustomerNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `InternalNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `CancellationReason` varchar(500) CHARACTER SET utf8mb4 NULL,
        `ConfirmedAtUtc` datetime(6) NULL,
        `StartedAtUtc` datetime(6) NULL,
        `CompletedAtUtc` datetime(6) NULL,
        `CancelledAtUtc` datetime(6) NULL,
        `NoShowAtUtc` datetime(6) NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_appointments` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_appointments_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_appointments_customerorders_CustomerOrderId` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorders` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_appointments_menuitems_MenuItemId` FOREIGN KEY (`MenuItemId`) REFERENCES `menuitems` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_appointments_pets_PetId` FOREIGN KEY (`PetId`) REFERENCES `pets` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_appointments_users_AssignedUserId` FOREIGN KEY (`AssignedUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_appointments_AssignedUserId` ON `appointments` (`AssignedUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_appointments_CompanyId_PetId_StartsAtUtc` ON `appointments` (`CompanyId`, `PetId`, `StartsAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_appointments_CompanyId_StartsAtUtc_Status` ON `appointments` (`CompanyId`, `StartsAtUtc`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_appointments_CustomerOrderId` ON `appointments` (`CustomerOrderId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_appointments_MenuItemId` ON `appointments` (`MenuItemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_appointments_PetId` ON `appointments` (`PetId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_pets_CompanyId_CustomerProfileId_IsActive` ON `pets` (`CompanyId`, `CustomerProfileId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_pets_CompanyId_Name` ON `pets` (`CompanyId`, `Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    CREATE INDEX `IX_pets_CustomerProfileId` ON `pets` (`CustomerProfileId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731145603_PetShopBackendIntegration') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260731145603_PetShopBackendIntegration', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `companies` ADD `AppointmentEndTime` time NOT NULL DEFAULT '18:00:00';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `companies` ADD `AppointmentServiceDays` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT '1,2,3,4,5,6';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `companies` ADD `AppointmentSlotIntervalMinutes` int NOT NULL DEFAULT 30;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `companies` ADD `AppointmentStartTime` time NOT NULL DEFAULT '08:00:00';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `companies` ADD `PetShopPublicCode` varchar(48) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `appointments` ADD `PublicAccessExpiresAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `appointments` ADD `PublicAccessRevokedAtUtc` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    ALTER TABLE `appointments` ADD `PublicAccessTokenHash` varchar(128) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE TABLE `appointmentblocks` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `AssignedUserId` char(36) COLLATE ascii_general_ci NULL,
        `StartsAtUtc` datetime(6) NOT NULL,
        `EndsAtUtc` datetime(6) NOT NULL,
        `Reason` varchar(300) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_appointmentblocks` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_appointmentblocks_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_appointmentblocks_users_AssignedUserId` FOREIGN KEY (`AssignedUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE TABLE `appointmentstatushistories` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `CompanyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `AppointmentId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ChangedByUserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `PreviousStatus` int NOT NULL,
        `NewStatus` int NOT NULL,
        `ChangedAtUtc` datetime(6) NOT NULL,
        `Reason` varchar(500) CHARACTER SET utf8mb4 NULL,
        `CreatedAtUtc` datetime(6) NOT NULL,
        `UpdatedAtUtc` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `TenantId` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_appointmentstatushistories` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_appointmentstatushistories_appointments_AppointmentId` FOREIGN KEY (`AppointmentId`) REFERENCES `appointments` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_appointmentstatushistories_users_ChangedByUserId` FOREIGN KEY (`ChangedByUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE UNIQUE INDEX `IX_companies_PetShopPublicCode` ON `companies` (`PetShopPublicCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE UNIQUE INDEX `IX_appointments_PublicAccessTokenHash` ON `appointments` (`PublicAccessTokenHash`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE INDEX `IX_appointmentblocks_AssignedUserId` ON `appointmentblocks` (`AssignedUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE INDEX `IX_appointmentblocks_CompanyId_StartsAtUtc_EndsAtUtc` ON `appointmentblocks` (`CompanyId`, `StartsAtUtc`, `EndsAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE INDEX `IX_appointmentstatushistories_AppointmentId` ON `appointmentstatushistories` (`AppointmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE INDEX `IX_appointmentstatushistories_ChangedByUserId` ON `appointmentstatushistories` (`ChangedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    CREATE INDEX `IX_appointmentstatushistories_CompanyId_AppointmentId_ChangedAt~` ON `appointmentstatushistories` (`CompanyId`, `AppointmentId`, `ChangedAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__efmigrationshistory` WHERE `MigrationId` = '20260731153413_CompletePetShopBackend') THEN

    INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260731153413_CompletePetShopBackend', '8.0.22');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

DROP PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`;

DROP PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`;

