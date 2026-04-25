-- Script chuyển đổi dữ liệu cột Author (String) sang JSON
-- Chạy script này trên PostgreSQL của server deploy

-- 1. Bảng podcasts
UPDATE podcasts
SET author = '{"name": "' || author || '"}'
WHERE author IS NOT NULL 
  AND author != ''
  AND author NOT LIKE '{%' 
  AND author NOT LIKE '[%';

-- 2. Bảng podcast_requests
UPDATE podcast_requests
SET author_info = '{"name": "' || author_info || '"}'
WHERE author_info IS NOT NULL 
  AND author_info != ''
  AND author_info NOT LIKE '{%' 
  AND author_info NOT LIKE '[%';

-- 3. Bảng PodcastEpisodeRequests (Chú ý bảng này được tạo bằng PascalCase trong EF Core)
UPDATE "PodcastEpisodeRequests"
SET "AuthorInfo" = '{"name": "' || "AuthorInfo" || '"}'
WHERE "AuthorInfo" IS NOT NULL 
  AND "AuthorInfo" != ''
  AND "AuthorInfo" NOT LIKE '{%' 
  AND "AuthorInfo" NOT LIKE '[%';
