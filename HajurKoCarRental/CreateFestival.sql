CREATE TABLE `Festivals` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) NOT NULL,
    `StartDate` datetime NOT NULL,
    `EndDate` datetime NOT NULL,
    `PriceMultiplier` decimal(18,2) NOT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
