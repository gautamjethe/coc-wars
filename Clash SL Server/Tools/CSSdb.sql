
-- MySQL dump 10.13  Distrib 8.0.16, for Win64 (x86_64)
--
-- Host: 192.168.1.7    Database: cssdb
-- ------------------------------------------------------
-- Server version	8.0.16

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
 SET NAMES utf8 ;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `Clan`
--

DROP VIEW IF EXISTS `clan`;
DROP TABLE IF EXISTS `Clan`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `Clan` (
  `ClanId` bigint(20) NOT NULL COMMENT 'The ID of the Clan',
  `LastUpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'The last time when this clan has been loaded',
  `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL COMMENT 'The infos and members data of the clan',
  PRIMARY KEY (`ClanId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;
CREATE OR REPLACE VIEW `clan` AS SELECT * FROM `Clan`;

--
-- Table structure for table `Player`
--

DROP VIEW IF EXISTS `player`;
DROP TABLE IF EXISTS `Player`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `Player` (
  `PlayerId` bigint(20) NOT NULL,
  `AccountStatus` tinyint(4) NOT NULL DEFAULT '0',
  `AccountPrivileges` tinyint(4) NOT NULL DEFAULT '0',
  `LastUpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IPAddress` varchar(255) DEFAULT NULL,
  `Avatar` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `GameObjects` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;
CREATE OR REPLACE VIEW `player` AS SELECT * FROM `Player`;

--
-- Table structure for table `ClanWar`
--

DROP VIEW IF EXISTS `clanwar`;
DROP TABLE IF EXISTS `ClanWar`;
SET character_set_client = utf8mb4 ;
CREATE TABLE `ClanWar` (
  `WarId` bigint(20) NOT NULL,
  `State` int(11) NOT NULL,
  `WarSize` int(11) NOT NULL,
  `Clan1Id` bigint(20) NOT NULL,
  `Clan1Name` varchar(255) NOT NULL,
  `Clan1Stars` int(11) NOT NULL DEFAULT '0',
  `Clan1Destruction` double NOT NULL DEFAULT '0',
  `Clan2Id` bigint(20) NOT NULL,
  `Clan2Name` varchar(255) NOT NULL,
  `Clan2Stars` int(11) NOT NULL DEFAULT '0',
  `Clan2Destruction` double NOT NULL DEFAULT '0',
  `PreparationStartTime` datetime DEFAULT NULL,
  `PreparationEndTime` datetime DEFAULT NULL,
  `BattleStartTime` datetime DEFAULT NULL,
  `BattleEndTime` datetime DEFAULT NULL,
  `WarEndedTime` datetime DEFAULT NULL,
  `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `LastUpdateTime` datetime DEFAULT NULL,
  PRIMARY KEY (`WarId`),
  KEY `idx_clan1` (`Clan1Id`),
  KEY `idx_clan2` (`Clan2Id`),
  KEY `idx_state` (`State`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE OR REPLACE VIEW `clanwar` AS SELECT * FROM `ClanWar`;

--
-- Table structure for table `ClanWarLog`
--

DROP VIEW IF EXISTS `clanwarlog`;
DROP TABLE IF EXISTS `ClanWarLog`;
SET character_set_client = utf8mb4 ;
CREATE TABLE `ClanWarLog` (
  `LogId` bigint(20) NOT NULL AUTO_INCREMENT,
  `WarId` bigint(20) NOT NULL,
  `ClanId` bigint(20) NOT NULL,
  `OpponentClanId` bigint(20) NOT NULL,
  `OpponentClanName` varchar(255) NOT NULL,
  `OpponentClanBadge` int(11) NOT NULL DEFAULT '0',
  `OpponentClanLevel` int(11) NOT NULL DEFAULT '1',
  `WarSize` int(11) NOT NULL,
  `ClanStars` int(11) NOT NULL,
  `OpponentStars` int(11) NOT NULL,
  `ClanDestruction` double NOT NULL,
  `OpponentDestruction` double NOT NULL,
  `ClanAttacksUsed` int(11) NOT NULL DEFAULT '0',
  `OpponentAttacksUsed` int(11) NOT NULL DEFAULT '0',
  `ClanExperienceGained` int(11) NOT NULL DEFAULT '0',
  `Result` tinyint(4) NOT NULL COMMENT '1=Won, 2=Lost, 3=Draw',
  `WarEndedTime` datetime NOT NULL,
  `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`LogId`),
  KEY `idx_clan_history` (`ClanId`,`WarEndedTime`),
  KEY `idx_war` (`WarId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE OR REPLACE VIEW `clanwarlog` AS SELECT * FROM `ClanWarLog`;

--
-- Table structure for table `ClanWarAttack`
--

DROP VIEW IF EXISTS `clanwarattack`;
DROP TABLE IF EXISTS `ClanWarAttack`;
SET character_set_client = utf8mb4 ;
CREATE TABLE `ClanWarAttack` (
  `AttackId` bigint(20) NOT NULL AUTO_INCREMENT,
  `WarId` bigint(20) NOT NULL,
  `AttackerId` bigint(20) NOT NULL,
  `AttackerName` varchar(255) NOT NULL,
  `AttackerClanId` bigint(20) NOT NULL,
  `AttackerMapPosition` int(11) NOT NULL,
  `DefenderId` bigint(20) NOT NULL,
  `DefenderName` varchar(255) NOT NULL,
  `DefenderClanId` bigint(20) NOT NULL,
  `DefenderMapPosition` int(11) NOT NULL,
  `Stars` int(11) NOT NULL DEFAULT '0',
  `DestructionPercentage` int(11) NOT NULL DEFAULT '0',
  `Duration` int(11) NOT NULL DEFAULT '0',
  `OrderNumber` int(11) NOT NULL DEFAULT '1',
  `AttackTime` datetime DEFAULT NULL,
  PRIMARY KEY (`AttackId`),
  KEY `idx_war_attacks` (`WarId`),
  KEY `idx_attacker` (`AttackerId`),
  KEY `idx_defender` (`DefenderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE OR REPLACE VIEW `clanwarattack` AS SELECT * FROM `ClanWarAttack`;

--
-- Table structure for table `ClanWarMember`
--

DROP VIEW IF EXISTS `clanwarmember`;
DROP TABLE IF EXISTS `ClanWarMember`;
SET character_set_client = utf8mb4 ;
CREATE TABLE `ClanWarMember` (
  `WarId` bigint(20) NOT NULL,
  `UserId` bigint(20) NOT NULL,
  `ClanId` bigint(20) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `TownHallLevel` int(11) NOT NULL,
  `MapPosition` int(11) NOT NULL,
  `AttacksUsed` int(11) NOT NULL DEFAULT '0',
  `StarsConceded` int(11) NOT NULL DEFAULT '0',
  `DamageConceded` int(11) NOT NULL DEFAULT '0',
  `GotAttackedCount` int(11) NOT NULL DEFAULT '0',
  `GoldGain` int(11) NOT NULL DEFAULT '0',
  `ElixirGain` int(11) NOT NULL DEFAULT '0',
  `DarkElixirGain` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`WarId`,`UserId`),
  KEY `idx_clan` (`ClanId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE OR REPLACE VIEW `clanwarmember` AS SELECT * FROM `ClanWarMember`;

--
-- Table structure for table `LeaderboardSeason`
--

DROP VIEW IF EXISTS `leaderboardseason`;
DROP TABLE IF EXISTS `LeaderboardSeason`;
SET character_set_client = utf8mb4 ;
CREATE TABLE `LeaderboardSeason` (
  `SeasonType` varchar(32) NOT NULL,
  `SeasonId` int(11) NOT NULL DEFAULT '1',
  `StartTime` datetime NOT NULL,
  `EndTime` datetime NOT NULL,
  `DurationSeconds` int(11) NOT NULL,
  `LastUpdateTime` datetime DEFAULT NULL,
  PRIMARY KEY (`SeasonType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE OR REPLACE VIEW `leaderboardseason` AS SELECT * FROM `LeaderboardSeason`;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-10-19 13:20:00

