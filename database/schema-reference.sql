-- EasyPark schema equivalent to migration 20261025150000_InitialCreate (reference only).
-- The API applies the EF Core migration automatically; do not run this script on the same database.

CREATE TABLE `user_accounts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `email` varchar(254) NOT NULL,
  `password_hash` varchar(100) NOT NULL,
  `role` varchar(32) NOT NULL,
  `status` varchar(16) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `last_login_at` datetime(6) NULL,
  CONSTRAINT `pk_user_accounts` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `profiles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_account_id` int NOT NULL,
  `first_name` varchar(100) NOT NULL,
  `last_name` varchar(120) NOT NULL,
  `phone` varchar(40) NOT NULL,
  `photo_url` varchar(1000) NULL,
  `company_name` varchar(180) NULL,
  `job_title` varchar(120) NULL,
  `created_at` datetime(6) NOT NULL,
  CONSTRAINT `pk_profiles` PRIMARY KEY (`id`),
  CONSTRAINT `fk_profiles_user_accounts_user_account_id` FOREIGN KEY (`user_account_id`) REFERENCES `user_accounts` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `vehicles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `profile_id` int NULL,
  `plate_number` varchar(12) NOT NULL,
  `vehicle_type` varchar(16) NOT NULL,
  `color` varchar(60) NULL,
  `is_default` tinyint(1) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  CONSTRAINT `pk_vehicles` PRIMARY KEY (`id`),
  CONSTRAINT `fk_vehicles_profiles_profile_id` FOREIGN KEY (`profile_id`) REFERENCES `profiles` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `parking_facilities` (
  `id` int NOT NULL AUTO_INCREMENT,
  `operator_profile_id` int NOT NULL,
  `name` varchar(180) NOT NULL,
  `address` varchar(300) NOT NULL,
  `latitude` decimal(10,7) NULL,
  `longitude` decimal(10,7) NULL,
  `hourly_rate` decimal(10,2) NOT NULL,
  `open_time` varchar(5) NOT NULL,
  `close_time` varchar(5) NOT NULL,
  `status` varchar(16) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  CONSTRAINT `pk_parking_facilities` PRIMARY KEY (`id`),
  CONSTRAINT `fk_parking_facilities_profiles_operator_profile_id` FOREIGN KEY (`operator_profile_id`) REFERENCES `profiles` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `parking_spots` (
  `id` int NOT NULL AUTO_INCREMENT,
  `facility_id` int NOT NULL,
  `code` varchar(30) NOT NULL,
  `level` int NOT NULL,
  `spot_type` varchar(16) NOT NULL,
  `status` varchar(16) NOT NULL,
  `version` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  CONSTRAINT `pk_parking_spots` PRIMARY KEY (`id`),
  CONSTRAINT `fk_parking_spots_parking_facilities_facility_id` FOREIGN KEY (`facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `reservations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(20) NOT NULL,
  `driver_profile_id` int NOT NULL,
  `vehicle_id` int NOT NULL,
  `parking_facility_id` int NOT NULL,
  `parking_spot_id` int NOT NULL,
  `start_at` datetime(6) NOT NULL,
  `duration_minutes` int NOT NULL,
  `estimated_amount` decimal(10,2) NOT NULL,
  `currency` varchar(3) NOT NULL,
  `status` varchar(16) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  CONSTRAINT `pk_reservations` PRIMARY KEY (`id`),
  CONSTRAINT `fk_reservations_profiles_driver_profile_id` FOREIGN KEY (`driver_profile_id`) REFERENCES `profiles` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_reservations_vehicles_vehicle_id` FOREIGN KEY (`vehicle_id`) REFERENCES `vehicles` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_reservations_parking_facilities_parking_facility_id` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_reservations_parking_spots_parking_spot_id` FOREIGN KEY (`parking_spot_id`) REFERENCES `parking_spots` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `access_movements` (
  `id` int NOT NULL AUTO_INCREMENT,
  `parking_facility_id` int NOT NULL,
  `vehicle_id` int NOT NULL,
  `plate_number` varchar(12) NOT NULL,
  `operator_id` int NOT NULL,
  `movement_type` varchar(8) NOT NULL,
  `registration_method` varchar(16) NOT NULL,
  `status` varchar(16) NOT NULL,
  `occurred_at` datetime(6) NOT NULL,
  `note` varchar(200) NULL,
  CONSTRAINT `pk_access_movements` PRIMARY KEY (`id`),
  CONSTRAINT `fk_access_movements_parking_facilities_parking_facility_id` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_access_movements_vehicles_vehicle_id` FOREIGN KEY (`vehicle_id`) REFERENCES `vehicles` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_access_movements_user_accounts_operator_id` FOREIGN KEY (`operator_id`) REFERENCES `user_accounts` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `parking_stays` (
  `id` int NOT NULL AUTO_INCREMENT,
  `parking_facility_id` int NOT NULL,
  `parking_spot_id` int NOT NULL,
  `vehicle_id` int NOT NULL,
  `reservation_id` int NULL,
  `entry_movement_id` int NOT NULL,
  `exit_movement_id` int NULL,
  `charged_amount` decimal(10,2) NULL,
  `currency` varchar(3) NOT NULL,
  `started_at` datetime(6) NOT NULL,
  `ended_at` datetime(6) NULL,
  CONSTRAINT `pk_parking_stays` PRIMARY KEY (`id`),
  CONSTRAINT `fk_parking_stays_parking_facilities_parking_facility_id` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_parking_stays_parking_spots_parking_spot_id` FOREIGN KEY (`parking_spot_id`) REFERENCES `parking_spots` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_parking_stays_vehicles_vehicle_id` FOREIGN KEY (`vehicle_id`) REFERENCES `vehicles` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_parking_stays_reservations_reservation_id` FOREIGN KEY (`reservation_id`) REFERENCES `reservations` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_parking_stays_access_movements_entry_movement_id` FOREIGN KEY (`entry_movement_id`) REFERENCES `access_movements` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_parking_stays_access_movements_exit_movement_id` FOREIGN KEY (`exit_movement_id`) REFERENCES `access_movements` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `ix_user_accounts_email` ON `user_accounts` (`email`);
CREATE UNIQUE INDEX `ix_profiles_user_account_id` ON `profiles` (`user_account_id`);
CREATE UNIQUE INDEX `ix_vehicles_plate_number` ON `vehicles` (`plate_number`);
CREATE INDEX `ix_vehicles_profile_id` ON `vehicles` (`profile_id`);
CREATE INDEX `ix_parking_facilities_operator_profile_id` ON `parking_facilities` (`operator_profile_id`);
CREATE UNIQUE INDEX `ix_parking_spots_facility_id_code` ON `parking_spots` (`facility_id`, `code`);
CREATE UNIQUE INDEX `ix_reservations_code` ON `reservations` (`code`);
CREATE INDEX `ix_reservations_driver_profile_id` ON `reservations` (`driver_profile_id`);
CREATE INDEX `ix_reservations_vehicle_id_status` ON `reservations` (`vehicle_id`, `status`);
CREATE INDEX `ix_reservations_parking_spot_id_status` ON `reservations` (`parking_spot_id`, `status`);
CREATE INDEX `ix_reservations_parking_facility_id_start_at` ON `reservations` (`parking_facility_id`, `start_at`);
CREATE INDEX `ix_access_movements_parking_facility_id_occurred_at` ON `access_movements` (`parking_facility_id`, `occurred_at`);
CREATE INDEX `ix_access_movements_vehicle_id` ON `access_movements` (`vehicle_id`);
CREATE INDEX `ix_access_movements_operator_id` ON `access_movements` (`operator_id`);
CREATE UNIQUE INDEX `ix_parking_stays_entry_movement_id` ON `parking_stays` (`entry_movement_id`);
CREATE UNIQUE INDEX `ix_parking_stays_exit_movement_id` ON `parking_stays` (`exit_movement_id`);
CREATE INDEX `ix_parking_stays_vehicle_id_exit_movement_id` ON `parking_stays` (`vehicle_id`, `exit_movement_id`);
CREATE INDEX `ix_parking_stays_parking_spot_id_exit_movement_id` ON `parking_stays` (`parking_spot_id`, `exit_movement_id`);
CREATE INDEX `ix_parking_stays_parking_facility_id` ON `parking_stays` (`parking_facility_id`);
CREATE INDEX `ix_parking_stays_reservation_id` ON `parking_stays` (`reservation_id`);
