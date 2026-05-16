-- Description: Print the row count of every base table in the open database.
-- Uses the sqlite_master catalog to discover table names, then issues one
-- SELECT COUNT(*) per table. Result format: one row per table, two columns
-- (table name, row count).

SELECT 'categories'    AS table_name, COUNT(*) AS row_count FROM categories
UNION ALL
SELECT 'customers',    COUNT(*) FROM customers
UNION ALL
SELECT 'employees',    COUNT(*) FROM employees
UNION ALL
SELECT 'order_details',COUNT(*) FROM order_details
UNION ALL
SELECT 'orders',       COUNT(*) FROM orders
UNION ALL
SELECT 'products',     COUNT(*) FROM products
UNION ALL
SELECT 'shippers',     COUNT(*) FROM shippers
UNION ALL
SELECT 'suppliers',    COUNT(*) FROM suppliers
ORDER BY row_count DESC;
