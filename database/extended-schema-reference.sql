-- REFERENCE ONLY: migration 20261102000000_ExtendedFeatures creates these seven tables.
-- Do not run on a database where EF Core migrations are enabled.

CREATE TABLE `alert_rules` (
`id` int NOT NULL AUTO_INCREMENT, `parking_facility_id` int NOT NULL,
`alert_type` varchar(50) NOT NULL, `severity` varchar(12) NOT NULL,
`threshold` int NOT NULL, `enabled` tinyint(1) NOT NULL, `updated_at` datetime(6) NOT NULL,
PRIMARY KEY (`id`), UNIQUE KEY `ix_alert_rules_facility_type` (`parking_facility_id`,`alert_type`),
CONSTRAINT `fk_alert_rules_facilities` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE `alerts` (
`id` int NOT NULL AUTO_INCREMENT, `parking_facility_id` int NOT NULL,
`alert_type` varchar(50) NOT NULL, `severity` varchar(12) NOT NULL,
`status` varchar(16) NOT NULL, `context_value` int NULL,
`parking_stay_id` int NULL, `access_movement_id` int NULL,
`created_at` datetime(6) NOT NULL, `resolved_at` datetime(6) NULL,
`resolved_by` int NULL, `note` varchar(500) NULL,
PRIMARY KEY (`id`), KEY `ix_alerts_facility_status` (`parking_facility_id`,`status`),
KEY `ix_alerts_parking_stay_id` (`parking_stay_id`),
KEY `ix_alerts_access_movement_id` (`access_movement_id`),
CONSTRAINT `fk_alerts_facilities` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_alerts_stays` FOREIGN KEY (`parking_stay_id`) REFERENCES `parking_stays` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_alerts_movements` FOREIGN KEY (`access_movement_id`) REFERENCES `access_movements` (`id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE `notification_templates` (
`id` int NOT NULL AUTO_INCREMENT, `type` varchar(60) NOT NULL, `locale` varchar(12) NOT NULL,
`title_template` varchar(250) NOT NULL, `body_template` varchar(2000) NOT NULL,
PRIMARY KEY (`id`), UNIQUE KEY `ix_notification_templates_type_locale` (`type`,`locale`)
) CHARACTER SET utf8mb4;

CREATE TABLE `notifications` (
`id` int NOT NULL AUTO_INCREMENT, `recipient_id` int NOT NULL,
`notification_template_id` int NOT NULL, `reservation_id` int NULL,
`parking_stay_id` int NULL, `alert_id` int NULL,
`title` varchar(250) NOT NULL, `message` varchar(2000) NOT NULL,
`channel` varchar(24) NOT NULL, `status` varchar(16) NOT NULL,
`created_at` datetime(6) NOT NULL, `sent_at` datetime(6) NULL, `read_at` datetime(6) NULL,
PRIMARY KEY (`id`), KEY `ix_notifications_recipient_created` (`recipient_id`,`created_at`),
KEY `ix_notifications_template` (`notification_template_id`), KEY `ix_notifications_reservation` (`reservation_id`),
KEY `ix_notifications_stay` (`parking_stay_id`), KEY `ix_notifications_alert` (`alert_id`),
CONSTRAINT `fk_notifications_users` FOREIGN KEY (`recipient_id`) REFERENCES `user_accounts` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_notifications_templates` FOREIGN KEY (`notification_template_id`) REFERENCES `notification_templates` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_notifications_reservations` FOREIGN KEY (`reservation_id`) REFERENCES `reservations` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_notifications_stays` FOREIGN KEY (`parking_stay_id`) REFERENCES `parking_stays` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_notifications_alerts` FOREIGN KEY (`alert_id`) REFERENCES `alerts` (`id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE `occupancy_metrics` (
`id` int NOT NULL AUTO_INCREMENT, `parking_facility_id` int NOT NULL,
`measured_at` datetime(6) NOT NULL, `total_spots` int NOT NULL, `occupied_spots` int NOT NULL,
`reserved_spots` int NOT NULL, `out_of_service_spots` int NOT NULL,
`occupancy_percent` decimal(6,2) NOT NULL,
`occupancy_rate` decimal(6,2) NOT NULL, `entry_count` int NOT NULL, `exit_count` int NOT NULL,
`average_stay_minutes` decimal(12,2) NOT NULL, `revenue` decimal(12,2) NOT NULL, `currency` varchar(3) NOT NULL,
PRIMARY KEY (`id`), UNIQUE KEY `ix_occupancy_metrics_facility_measured` (`parking_facility_id`,`measured_at`),
CONSTRAINT `fk_occupancy_metrics_facilities` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE `reports` (
`id` int NOT NULL AUTO_INCREMENT, `parking_facility_id` int NOT NULL,
`operator_id` int NOT NULL, `report_type` varchar(30) NOT NULL,
`period_start` datetime(6) NOT NULL, `period_end` datetime(6) NOT NULL,
`generated_at` datetime(6) NOT NULL, `total_movements` int NOT NULL,
`completed_stays` int NOT NULL, `revenue` decimal(12,2) NOT NULL,
PRIMARY KEY (`id`), KEY `ix_reports_facility_created` (`parking_facility_id`,`generated_at`),
KEY `ix_reports_operator_id` (`operator_id`),
CONSTRAINT `fk_reports_facilities` FOREIGN KEY (`parking_facility_id`) REFERENCES `parking_facilities` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_reports_users` FOREIGN KEY (`operator_id`) REFERENCES `user_accounts` (`id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

CREATE TABLE `refresh_tokens` (
`id` int NOT NULL AUTO_INCREMENT, `user_account_id` int NOT NULL,
`token_hash` varchar(64) NOT NULL, `created_at` datetime(6) NOT NULL,
`expires_at` datetime(6) NOT NULL, `revoked_at` datetime(6) NULL,
PRIMARY KEY (`id`), UNIQUE KEY `ix_refresh_tokens_hash` (`token_hash`),
KEY `ix_refresh_tokens_user_expiry` (`user_account_id`,`expires_at`),
CONSTRAINT `fk_refresh_tokens_users` FOREIGN KEY (`user_account_id`) REFERENCES `user_accounts` (`id`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;
