CREATE SCHEMA IF NOT EXISTS identity;

-- Roles
CREATE TABLE IF NOT EXISTS identity.roles
(
    id   uuid                  NOT NULL,
    name character varying(16) NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_roles_name ON identity.roles (name);

-- Users
CREATE TABLE IF NOT EXISTS identity.users
(
    id                     uuid                     NOT NULL,
    email                  character varying(256)   NOT NULL,
    normalized_email       character varying(256)   NOT NULL GENERATED ALWAYS AS (LOWER(email)) STORED,
    email_confirmed        boolean                  NOT NULL DEFAULT false,
    phone_number           character varying(20)    NULL,
    phone_number_confirmed boolean                  NOT NULL DEFAULT false,
    two_factor_enabled     boolean                  NOT NULL DEFAULT false,
    two_factor_secret      character varying(64)    NULL,
    password_hash          text                     NOT NULL,
    culture                character varying(5)     NOT NULL,
    created_at             timestamp with time zone NOT NULL,
    updated_at             timestamp with time zone NOT NULL,
    CONSTRAINT pk_users PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_normalized_email ON identity.users (normalized_email);
CREATE INDEX IF NOT EXISTS ix_users_created_at ON identity.users (created_at);

-- User Roles
CREATE TABLE IF NOT EXISTS identity.user_roles
(
    user_id    uuid                     NOT NULL,
    role_id    uuid                     NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_roles_role_id FOREIGN KEY (role_id) REFERENCES identity.roles (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_users_user_id FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_roles_role_id ON identity.user_roles (role_id);

-- User External Logins
CREATE TABLE IF NOT EXISTS identity.user_external_logins
(
    id                    uuid                     NOT NULL,
    user_id               uuid                     NOT NULL,
    provider              character varying(32)    NOT NULL,
    provider_key          character varying(256)   NOT NULL,
    provider_display_name character varying(256)   NULL,
    created_at            timestamp with time zone NOT NULL,
    updated_at            timestamp with time zone NOT NULL,
    CONSTRAINT pk_user_external_logins PRIMARY KEY (id),
    CONSTRAINT fk_user_external_logins_users_user_id FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_user_external_logins_provider_provider_key ON identity.user_external_logins (provider, provider_key);
CREATE INDEX IF NOT EXISTS ix_user_external_logins_user_id ON identity.user_external_logins (user_id);

-- User Sessions
CREATE TABLE IF NOT EXISTS identity.user_sessions
(
    id                uuid                     NOT NULL,
    ip_address        character varying(45)    NULL,
    user_agent        character varying(512)   NOT NULL,
    status            character varying(16)    NOT NULL,
    access_token_id   uuid                     NOT NULL,
    external_login_id uuid                     NULL,
    refresh_token     character varying(44)    NOT NULL,
    expired_at        timestamp with time zone NOT NULL,
    user_id           uuid                     NOT NULL,
    created_at        timestamp with time zone NOT NULL,
    updated_at        timestamp with time zone NOT NULL,
    CONSTRAINT pk_user_sessions PRIMARY KEY (id),
    CONSTRAINT fk_user_sessions_users_user_id FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_sessions_user_external_logins_external_login_id FOREIGN KEY (external_login_id) REFERENCES identity.user_external_logins (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_sessions_user_id ON identity.user_sessions (user_id);
CREATE INDEX IF NOT EXISTS ix_user_sessions_external_login_id ON identity.user_sessions (external_login_id);

-- User Recovery Codes
CREATE TABLE IF NOT EXISTS identity.user_recovery_codes
(
    id          uuid                     NOT NULL,
    user_id     uuid                     NOT NULL,
    code_hash   character varying(64)    NOT NULL,
    redeemed_at timestamp with time zone NULL,
    created_at  timestamp with time zone NOT NULL,
    CONSTRAINT pk_user_recovery_codes PRIMARY KEY (id),
    CONSTRAINT fk_user_recovery_codes_users_user_id FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_user_recovery_codes_user_id ON identity.user_recovery_codes (user_id);
