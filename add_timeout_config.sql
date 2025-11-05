-- PostgreSQL Script to add ORDER_PAYMENT_TIMEOUT_HOURS system configuration
-- This configures the timeout period for unpaid orders (default: 1 hour)

INSERT INTO "SystemConfig" ("Id", "Key", "Value", "Description", "CreatedAt", "UpdatedAt", "IsDeleted") 
VALUES (
    gen_random_uuid(), 
    'ORDER_PAYMENT_TIMEOUT_HOURS', 
    '1', 
    'Number of hours before unpaid orders are automatically cancelled', 
    NOW(), 
    NOW(), 
    false
);

-- Verify the insertion
SELECT * FROM "SystemConfig" WHERE "Key" = 'ORDER_PAYMENT_TIMEOUT_HOURS';
