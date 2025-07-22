-- 創建資料庫 (如果不存在)
CREATE DATABASE translatebot;

-- 連接到 translatebot 資料庫後執行以下命令
\c translatebot;

-- 啟用 pgvector 擴展
CREATE EXTENSION IF NOT EXISTS vector;

-- 創建 address_references 表
CREATE TABLE IF NOT EXISTS address_references (
    id SERIAL PRIMARY KEY,
    content VARCHAR(1000) NOT NULL,
    type VARCHAR(100) NOT NULL,
    embedding vector (768) NOT NULL,
    created_at TIMESTAMP
    WITH
        TIME ZONE DEFAULT NOW ()
);

-- 為向量相似性搜索創建索引
CREATE INDEX IF NOT EXISTS ix_address_references_embedding ON address_references USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

-- 創建用於內容搜索的索引
CREATE INDEX IF NOT EXISTS ix_address_references_content ON address_references (content);

CREATE INDEX IF NOT EXISTS ix_address_references_type ON address_references(type);