-- ============================================================
-- Customer Support Ticket System
-- MySQL Database Schema + Seed Data
-- ============================================================

CREATE DATABASE IF NOT EXISTS SupportTicketDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE SupportTicketDB;

-- ============================================================
-- Table: Users
-- ============================================================
CREATE TABLE IF NOT EXISTS Users (
    Id           INT AUTO_INCREMENT PRIMARY KEY,
    Username     VARCHAR(100)  NOT NULL UNIQUE,
    PasswordHash VARCHAR(256)  NOT NULL,
    FullName     VARCHAR(200)  NOT NULL,
    Email        VARCHAR(200)  NOT NULL UNIQUE,
    Role         ENUM('User','Admin') NOT NULL DEFAULT 'User',
    IsActive     TINYINT(1)    NOT NULL DEFAULT 1,
    CreatedAt    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Table: Tickets
-- ============================================================
CREATE TABLE IF NOT EXISTS Tickets (
    Id               INT AUTO_INCREMENT PRIMARY KEY,
    TicketNumber     VARCHAR(20)  NOT NULL UNIQUE,
    Subject          VARCHAR(300) NOT NULL,
    Description      TEXT         NOT NULL,
    Priority         ENUM('Low','Medium','High')        NOT NULL DEFAULT 'Medium',
    Status           ENUM('Open','InProgress','Closed') NOT NULL DEFAULT 'Open',
    CreatedByUserId  INT          NOT NULL,
    AssignedToUserId INT          NULL,
    CreatedAt        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_ticket_createdby  FOREIGN KEY (CreatedByUserId)  REFERENCES Users(Id),
    CONSTRAINT fk_ticket_assignedto FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id)
);

-- ============================================================
-- Table: TicketStatusHistory
-- ============================================================
CREATE TABLE IF NOT EXISTS TicketStatusHistory (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    TicketId        INT          NOT NULL,
    OldStatus       VARCHAR(50)  NULL,
    NewStatus       VARCHAR(50)  NOT NULL,
    ChangedByUserId INT          NOT NULL,
    ChangedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Remarks         VARCHAR(500) NULL,
    CONSTRAINT fk_history_ticket    FOREIGN KEY (TicketId)        REFERENCES Tickets(Id),
    CONSTRAINT fk_history_changedby FOREIGN KEY (ChangedByUserId) REFERENCES Users(Id)
);

-- ============================================================
-- Table: TicketComments
-- ============================================================
CREATE TABLE IF NOT EXISTS TicketComments (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    TicketId        INT        NOT NULL,
    CommentText     TEXT       NOT NULL,
    IsInternal      TINYINT(1) NOT NULL DEFAULT 0,
    CreatedByUserId INT        NOT NULL,
    CreatedAt       DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_comment_ticket FOREIGN KEY (TicketId)        REFERENCES Tickets(Id),
    CONSTRAINT fk_comment_user   FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

-- ============================================================
-- Seed Data
-- ============================================================
SET SQL_SAFE_UPDATES = 0;

DELETE FROM TicketComments;
DELETE FROM TicketStatusHistory;
DELETE FROM Tickets;
DELETE FROM Users;

ALTER TABLE Users               AUTO_INCREMENT = 1;
ALTER TABLE Tickets             AUTO_INCREMENT = 1;
ALTER TABLE TicketStatusHistory AUTO_INCREMENT = 1;
ALTER TABLE TicketComments      AUTO_INCREMENT = 1;

-- ============================================================
-- Users
-- admin  → password: admin123
-- others → password: user123
-- Hashes generated using BCrypt cost factor 11
-- ============================================================
INSERT INTO Users (Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt) VALUES
('admin',  '$2a$11$O1yJP/skeHh/wgDH/GiLletip7uxFN5uE2q32GPJPhy2FTn5IfsDu', 'System Administrator', 'admin@support.com',  'Admin', 1, UTC_TIMESTAMP()),
('jsmith', '$2a$11$yKUyuE06jsOEK1xhXxHUTOecV1WCIefEuo/D5yh./J2kqDp5BzdQ.', 'John Smith',           'jsmith@example.com', 'Admin', 1, UTC_TIMESTAMP()),
('alice',  '$2a$11$yKUyuE06jsOEK1xhXxHUTOecV1WCIefEuo/D5yh./J2kqDp5BzdQ.', 'Alice Johnson',        'alice@example.com',  'User',  1, UTC_TIMESTAMP()),
('bob',    '$2a$11$yKUyuE06jsOEK1xhXxHUTOecV1WCIefEuo/D5yh./J2kqDp5BzdQ.', 'Bob Williams',         'bob@example.com',    'User',  1, UTC_TIMESTAMP());

SET SQL_SAFE_UPDATES = 1;

-- ============================================================
-- Verify setup
-- ============================================================
SELECT Id, Username, Role, IsActive FROM Users;
SELECT 'Database setup complete!' AS Status;

-- ============================================================
-- Login Credentials:
--   admin  / admin123  (Admin)
--   jsmith / user123   (Admin)
--   alice  / user123   (User)
--   bob    / user123   (User)
-- ============================================================