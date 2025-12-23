CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE otp_codes (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    email character varying(255) NOT NULL,
    code character varying(10) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    is_used boolean NOT NULL DEFAULT FALSE,
    used_at timestamp with time zone,
    purpose integer NOT NULL,
    CONSTRAINT otp_codes_pkey PRIMARY KEY (id)
);

CREATE TABLE outbox_messages (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    type character varying(255) NOT NULL,
    payload text NOT NULL,
    occurred_on_utc timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    processed_on_utc timestamp with time zone,
    error text,
    CONSTRAINT outbox_messages_pkey PRIMARY KEY (id)
);

CREATE TABLE user_roles (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    name text NOT NULL,
    CONSTRAINT user_roles_pkey PRIMARY KEY (id)
);

CREATE TABLE users (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    role_id uuid,
    username text NOT NULL,
    email text NOT NULL,
    password text NOT NULL,
    full_name text,
    created_at timestamp with time zone DEFAULT (now() at time zone 'utc'),
    updated_at timestamp with time zone,
    CONSTRAINT users_pkey PRIMARY KEY (id),
    CONSTRAINT users_role_id_fkey FOREIGN KEY (role_id) REFERENCES user_roles (id)
);

CREATE TABLE oauthaccount (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    user_id uuid,
    provider text NOT NULL,
    provider_account_id text NOT NULL,
    CONSTRAINT oauthaccount_pkey PRIMARY KEY (id),
    CONSTRAINT oauthaccount_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE refresh_tokens (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    user_id uuid NOT NULL,
    token character varying(500) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    is_revoked boolean NOT NULL DEFAULT FALSE,
    revoked_at timestamp with time zone,
    CONSTRAINT refresh_tokens_pkey PRIMARY KEY (id),
    CONSTRAINT refresh_tokens_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX "IX_oauthaccount_user_id" ON oauthaccount (user_id);

CREATE INDEX otp_codes_email_code_purpose_idx ON otp_codes (email, code, purpose);

CREATE UNIQUE INDEX refresh_tokens_token_key ON refresh_tokens (token);

CREATE INDEX refresh_tokens_user_id_idx ON refresh_tokens (user_id);

CREATE UNIQUE INDEX user_roles_name_key ON user_roles (name);

CREATE INDEX "IX_users_role_id" ON users (role_id);

CREATE UNIQUE INDEX users_email_key ON users (email);

CREATE UNIQUE INDEX users_username_key ON users (username);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251213091728_AddUpdatedAtToUser', '10.0.1');

COMMIT;

START TRANSACTION;
ALTER TABLE users ADD first_name text;

ALTER TABLE users ADD last_name text;


                UPDATE users
                SET 
                    first_name = CASE 
                        WHEN full_name IS NULL OR full_name = '' THEN NULL
                        WHEN position(' ' in full_name) > 0 THEN substring(full_name, 1, position(' ' in full_name) - 1)
                        ELSE full_name
                    END,
                    last_name = CASE 
                        WHEN full_name IS NULL OR full_name = '' THEN NULL
                        WHEN position(' ' in full_name) > 0 THEN substring(full_name, position(' ' in full_name) + 1)
                        ELSE NULL
                    END
                WHERE full_name IS NOT NULL;
            

ALTER TABLE users DROP COLUMN full_name;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251215141821_SplitFullNameToFirstNameLastName', '10.0.1');

COMMIT;

