BEGIN;
--drop if exists
DROP TABLE IF EXISTS user_upgrades;
DROP TABLE IF EXISTS upgrades;
DROP TABLE IF EXISTS users;

CREATE TABLE users (
    user_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	role VARCHAR(20) NOT NULL DEFAULT 'User',
    user_name VARCHAR(30)  NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(200) NOT NULL,
    cash_balance BIGINT NOT NULL DEFAULT 0,
    cash_per_click INT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

ALTER TABLE users
    ADD CONSTRAINT uq_users_user_name UNIQUE (user_name);

ALTER TABLE users
    ADD CONSTRAINT uq_users_email UNIQUE (email);

ALTER TABLE users
    ADD CONSTRAINT cs_users_cash_balance_nonnegative CHECK (cash_balance >= 0);

ALTER TABLE users
    ADD CONSTRAINT cs_users_cash_per_click_positive CHECK (cash_per_click >= 1);

CREATE TABLE upgrades (
    upgrade_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50)  NOT NULL,
    description VARCHAR(200) NOT NULL,
    image_url VARCHAR(200) NOT NULL,
    base_cost INT NOT NULL,
    cps_bonus INT NOT NULL DEFAULT 0,  -- cash per second per unit
    cpc_bonus INT NOT NULL DEFAULT 0,       -- cash per click per unit
    max_quantity INT NOT NULL DEFAULT 100,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

ALTER TABLE upgrades
    ADD CONSTRAINT uq_upgrades_name UNIQUE (name);

ALTER TABLE upgrades
    ADD CONSTRAINT cs_upgrades_base_cost_positive CHECK (base_cost > 0);

ALTER TABLE upgrades
    ADD CONSTRAINT cs_upgrades_bonuses_nonnegative CHECK (cps_bonus >= 0 AND cpc_bonus >= 0);

ALTER TABLE upgrades
    ADD CONSTRAINT cs_upgrades_max_quantity_positive CHECK (max_quantity >= 1);

CREATE TABLE user_upgrades (
    user_upgrade_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id INT NOT NULL,
    upgrade_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    total_spent  BIGINT NOT NULL DEFAULT 0,
    is_equipped BOOLEAN NOT NULL DEFAULT TRUE,
    last_purchased_at TIMESTAMPTZ NULL,

    CONSTRAINT fk_user_upgrades_users
        FOREIGN KEY (user_id) REFERENCES users(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_upgrades_upgrades
        FOREIGN KEY (upgrade_id) REFERENCES upgrades(upgrade_id)
        ON DELETE CASCADE
);

ALTER TABLE user_upgrades
    ADD CONSTRAINT uq_user_upgrades_user_upgrade UNIQUE (user_id, upgrade_id);
ALTER TABLE user_upgrades
    ADD CONSTRAINT cs_user_upgrades_quantity_nonnegative CHECK (quantity >= 0);
ALTER TABLE user_upgrades
    ADD CONSTRAINT cs_user_upgrades_total_spent_nonnegative CHECK (total_spent >= 0);

INSERT INTO upgrades (name, description, image_url, base_cost, cps_bonus, cpc_bonus, max_quantity, is_active)
VALUES
  ('Paid Voter', 'More votes equal more power and money.',   '/images/upgrades/voter.png',   20,     1,   1, 500, TRUE),
  ('Media Outlet', 'Buy out the Media, to remove dirt off your name.',  '/images/upgrades/media.png',  400,  2,  0, 500, TRUE),
  ('Crook Guards', 'Protection from bullets.',   '/images/upgrades/crook.png',  100,  3, 0,300, TRUE),
  ('Shadow Network', 'Moves funds to our sunnier jurisdiction.',  '/images/upgrades/network.png',  1000,  5, 0, 200, TRUE),
  ('"Independant" Political Party', 'Buy out an independant political party to lower competition.', '/images/upgrades/party.png',     5000,   10,  0, 100, TRUE),
  ('Country With Capidal D',  'Make the whole country work for your benefit.', '/images/upgrades/country.png',  20000, 25,   30,  1, TRUE);

COMMIT;
--ROLLBACK;
--UPDATE users SET role = 'Admin' WHERE email = 'your_email_here';