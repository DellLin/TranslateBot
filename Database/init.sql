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

-- 創建 translation_records 表
CREATE TABLE IF NOT EXISTS translation_records (
    id SERIAL PRIMARY KEY,
    original_text VARCHAR(500) NOT NULL,
    translated_text VARCHAR(500) NOT NULL,
    translation_type VARCHAR(50) NOT NULL,
    user_session VARCHAR(100),
    created_at TIMESTAMP
    WITH
        TIME ZONE DEFAULT NOW (),
        processing_time INTERVAL NOT NULL,
        is_successful BOOLEAN DEFAULT TRUE,
        error_message VARCHAR(1000)
);

-- 為常用查詢建立索引
CREATE INDEX IF NOT EXISTS ix_translation_records_created_at ON translation_records (created_at);

CREATE INDEX IF NOT EXISTS ix_translation_records_type ON translation_records (translation_type);

CREATE INDEX IF NOT EXISTS ix_translation_records_session ON translation_records (user_session);

CREATE INDEX IF NOT EXISTS ix_translation_records_original_text ON translation_records (original_text);

CREATE INDEX IF NOT EXISTS ix_translation_records_success ON translation_records (is_successful);