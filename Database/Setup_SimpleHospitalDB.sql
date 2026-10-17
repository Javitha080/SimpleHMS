-- ============================================================================
-- SimpleHMS - Hospital Management System database setup
-- ----------------------------------------------------------------------------
-- Idempotent setup script: safe to run more than once, creates the database
-- and tables with default file locations (no hardcoded paths), then seeds a
-- minimal set of data so the application is usable immediately.
--
-- Requires: SQL Server 2016+ (SSMS, sqlcmd, or any T-SQL runner).
-- Default login created by this script (change the password after first login!):
--     Username: admin
--     Password: admin123   (stored as a PBKDF2-SHA256 hash, 100k iterations)
--
-- For a different SQL Server instance than localhost\SQLEXPRESS, either edit
-- the connection used by the client (HMS_CONNECTION_STRING environment
-- variable) or connect to the target instance before running this script.
-- ============================================================================

USE [master];
GO

IF DB_ID(N'SimpleHospitalDB') IS NULL
BEGIN
    CREATE DATABASE [SimpleHospitalDB];
END
GO

IF DB_ID(N'SimpleHospitalDB') IS NULL
BEGIN
    RAISERROR(N'Failed to create database [SimpleHospitalDB]; aborting setup.', 20, 1) WITH LOG;
    RETURN;
END
GO

USE [SimpleHospitalDB];
GO

-- ----------------------------------------------------------------------------
-- Tables
-- ----------------------------------------------------------------------------

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserID      INT IDENTITY(1,1) NOT NULL,
        Username    NVARCHAR(50)      NOT NULL,
        Password    NVARCHAR(100)     NOT NULL,   -- PBKDF2 hash (PasswordHasher format)
        UserType    NVARCHAR(20)      NOT NULL,
        IsActive    BIT               NOT NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserID),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT CK_Users_UserType CHECK (UserType IN (N'Admin', N'Receptionist')),
        CONSTRAINT DF_Users_IsActive DEFAULT (1) FOR [IsActive]
    );
END
GO

IF OBJECT_ID(N'dbo.Patients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Patients
    (
        PatientID   INT IDENTITY(1,1) NOT NULL,
        Name        NVARCHAR(100)     NOT NULL,
        Age         INT               NOT NULL,
        Gender      NVARCHAR(10)      NOT NULL,
        Phone       NVARCHAR(15)      NOT NULL,
        Email       NVARCHAR(100)     NULL,
        Address     NVARCHAR(200)     NULL,
        CONSTRAINT PK_Patients PRIMARY KEY CLUSTERED (PatientID),
        CONSTRAINT CK_Patients_Age CHECK (Age BETWEEN 0 AND 150),
        CONSTRAINT CK_Patients_Gender CHECK (Gender IN (N'Male', N'Female'))
    );
END
GO

-- Upgrades databases created before the email column existed.
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.Patients') AND name = N'Email')
    ALTER TABLE dbo.Patients ADD Email NVARCHAR(100) NULL;
GO

IF OBJECT_ID(N'dbo.Doctors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doctors
    (
        DoctorID        INT IDENTITY(1,1) NOT NULL,
        Name            NVARCHAR(100)     NOT NULL,
        Specialization  NVARCHAR(100)     NOT NULL,
        Phone           NVARCHAR(15)      NOT NULL,
        Fee             DECIMAL(10,2)     NOT NULL,
        CONSTRAINT PK_Doctors PRIMARY KEY CLUSTERED (DoctorID),
        CONSTRAINT CK_Doctors_Fee CHECK (Fee >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments
    (
        AppointmentID    INT IDENTITY(1,1) NOT NULL,
        PatientID        INT               NOT NULL,
        DoctorID         INT               NOT NULL,
        AppointmentDate  DATE              NOT NULL,
        AppointmentTime  TIME(7)           NOT NULL,
        CONSTRAINT PK_Appointments PRIMARY KEY CLUSTERED (AppointmentID),
        CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientID) REFERENCES dbo.Patients (PatientID),
        CONSTRAINT FK_Appointments_Doctors  FOREIGN KEY (DoctorID)  REFERENCES dbo.Doctors  (DoctorID)
    );
END
GO

IF OBJECT_ID(N'dbo.Bills', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bills
    (
        BillID           INT IDENTITY(1,1) NOT NULL,
        BillNumber       NVARCHAR(20)      NOT NULL,
        PatientID        INT               NOT NULL,
        DoctorID         INT               NOT NULL,
        AppointmentID    INT               NULL,
        BillDate         DATETIME          NULL,
        ConsultationFee  DECIMAL(10,2)     NOT NULL,
        MedicineFee      DECIMAL(10,2)     NULL,
        TestFee          DECIMAL(10,2)     NULL,
        OtherFee         DECIMAL(10,2)     NULL,
        Discount         DECIMAL(10,2)     NULL,
        TotalAmount      DECIMAL(10,2)     NOT NULL,
        PaymentStatus    NVARCHAR(20)      NULL,
        PaymentMethod    NVARCHAR(50)      NULL,
        PaymentDate      DATETIME          NULL,
        Notes            NVARCHAR(500)     NULL,
        CONSTRAINT PK_Bills PRIMARY KEY CLUSTERED (BillID),
        CONSTRAINT UQ_Bills_BillNumber UNIQUE (BillNumber),
        CONSTRAINT FK_Bills_Patients FOREIGN KEY (PatientID)     REFERENCES dbo.Patients     (PatientID),
        CONSTRAINT FK_Bills_Doctors  FOREIGN KEY (DoctorID)      REFERENCES dbo.Doctors      (DoctorID),
        CONSTRAINT FK_Bills_Appointments FOREIGN KEY (AppointmentID) REFERENCES dbo.Appointments (AppointmentID),
        CONSTRAINT DF_Bills_BillDate       DEFAULT (getdate()) FOR [BillDate],
        CONSTRAINT DF_Bills_MedicineFee    DEFAULT (0) FOR [MedicineFee],
        CONSTRAINT DF_Bills_TestFee        DEFAULT (0) FOR [TestFee],
        CONSTRAINT DF_Bills_OtherFee       DEFAULT (0) FOR [OtherFee],
        CONSTRAINT DF_Bills_Discount       DEFAULT (0) FOR [Discount],
        CONSTRAINT DF_Bills_PaymentStatus  DEFAULT ('Pending') FOR [PaymentStatus],
        CONSTRAINT CK_Bills_PaymentStatus CHECK (PaymentStatus IN (N'Pending', N'Partial', N'Paid', N'Cancelled', N'Refunded'))
    );
END
GO

IF OBJECT_ID(N'dbo.BillItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillItems
    (
        ItemID       INT IDENTITY(1,1) NOT NULL,
        BillID       INT               NOT NULL,
        ItemName     NVARCHAR(200)     NOT NULL,
        ItemType     NVARCHAR(50)      NOT NULL,
        Quantity     INT               NULL,
        UnitPrice    DECIMAL(10,2)     NOT NULL,
        TotalPrice   DECIMAL(10,2)     NOT NULL,
        CONSTRAINT PK_BillItems PRIMARY KEY CLUSTERED (ItemID),
        CONSTRAINT FK_BillItems_Bills FOREIGN KEY (BillID) REFERENCES dbo.Bills (BillID) ON DELETE CASCADE,
        CONSTRAINT DF_BillItems_Quantity DEFAULT (1) FOR [Quantity]
    );
END
GO

-- ----------------------------------------------------------------------------
-- Helpful indexes (lookups used by the repositories)
-- ----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Appointments_PatientDoctorSlot')
    CREATE INDEX IX_Appointments_PatientDoctorSlot
        ON dbo.Appointments (PatientID, DoctorID, AppointmentDate, AppointmentTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bills_Patient')
    CREATE INDEX IX_Bills_Patient ON dbo.Bills (PatientID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BillItems_Bill')
    CREATE INDEX IX_BillItems_Bill ON dbo.BillItems (BillID);
GO

-- ----------------------------------------------------------------------------
-- Seed data
-- ----------------------------------------------------------------------------

-- Default administrator (PasswordHasher format, PBKDF2-SHA256, 100k iterations).
-- Password: admin123  -->  change it after first login.
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
    INSERT INTO dbo.Users (Username, Password, UserType, IsActive)
    VALUES (N'admin',
            N'PBKDF2$100000$YVE6Qpi91qMtcKG42Hi1lg==$xgfQfDM+wGxtgsqv8Ax6BTc4fJi8eeDLcF6nsyEMLa4=',
            N'Admin', 1);

-- Sample doctors
IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Name = N'Dr. Sarah Silva')
    INSERT INTO dbo.Doctors (Name, Specialization, Phone, Fee)
    VALUES (N'Dr. Sarah Silva', N'Cardiology', N'0771234567', 3500.00);

IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Name = N'Dr. Nuwan Perera')
    INSERT INTO dbo.Doctors (Name, Specialization, Phone, Fee)
    VALUES (N'Dr. Nuwan Perera', N'Pediatrics', N'0712345678', 2500.00);

-- Sample patients
IF NOT EXISTS (SELECT 1 FROM dbo.Patients WHERE Name = N'Kasun Fernando')
    INSERT INTO dbo.Patients (Name, Age, Gender, Phone, Email, Address)
    VALUES (N'Kasun Fernando', 32, N'Male', N'0751112222', N'kasun.fernando@example.com', N'12 Galle Road, Colombo 03');

IF NOT EXISTS (SELECT 1 FROM dbo.Patients WHERE Name = N'Nimali Jayasuriya')
    INSERT INTO dbo.Patients (Name, Age, Gender, Phone, Email, Address)
    VALUES (N'Nimali Jayasuriya', 24, N'Female', N'0763334444', N'nimali.jayasuriya@example.com', N'48 Kandy Road, Kadawatha');

-- Backfills patient emails on databases created before this column existed.
UPDATE dbo.Patients SET Email = N'kasun.fernando@example.com'
WHERE Name = N'Kasun Fernando' AND Email IS NULL;

UPDATE dbo.Patients SET Email = N'nimali.jayasuriya@example.com'
WHERE Name = N'Nimali Jayasuriya' AND Email IS NULL;

GO

PRINT N'Setup completed: SimpleHospitalDB created/verified and seed data inserted.';
