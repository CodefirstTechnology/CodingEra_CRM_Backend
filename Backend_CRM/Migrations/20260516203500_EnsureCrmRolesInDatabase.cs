using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class EnsureCrmRolesInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Model snapshot already included crm_roles after initially2, but Up() was empty — DB drifted.
            // Idempotent so safe if partially applied.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS crm_roles (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name character varying(128) NOT NULL,
    description text NOT NULL,
    is_active boolean NOT NULL,
    last_modified timestamp with time zone NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_crm_roles_name"" ON crm_roles (name);
");

            migrationBuilder.Sql(@"
INSERT INTO crm_roles (name, description, is_active, last_modified)
VALUES
  ('user', 'Standard CRM user', true, NOW() AT TIME ZONE 'utc'),
  ('admin', 'Full CRM access', true, NOW() AT TIME ZONE 'utc')
ON CONFLICT (name) DO NOTHING;
");

            migrationBuilder.Sql(@"ALTER TABLE users ADD COLUMN IF NOT EXISTS role_id integer;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'users' AND column_name = 'role'
  ) THEN
    UPDATE users u
    SET role_id = r.id
    FROM crm_roles r
    WHERE u.role_id IS NULL
      AND LOWER(TRIM(r.name)) = LOWER(TRIM(u.role))
      AND r.is_active = true;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_users_role_id"" ON users (role_id);");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_users_crm_roles_role_id'
  ) THEN
    ALTER TABLE users
      ADD CONSTRAINT ""FK_users_crm_roles_role_id""
      FOREIGN KEY (role_id) REFERENCES crm_roles (id) ON DELETE SET NULL;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"ALTER TABLE users DROP COLUMN IF EXISTS role;");

            migrationBuilder.Sql(@"ALTER TABLE leads DROP COLUMN IF EXISTS lead_owner_name;");
            migrationBuilder.Sql(@"ALTER TABLE leads DROP COLUMN IF EXISTS owner;");
            migrationBuilder.Sql(@"ALTER TABLE lead_histories DROP COLUMN IF EXISTS lead_owner_name;");
            migrationBuilder.Sql(@"ALTER TABLE lead_histories DROP COLUMN IF EXISTS owner;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration aligns the database with the model; Down is not supported.");
        }
    }
}
