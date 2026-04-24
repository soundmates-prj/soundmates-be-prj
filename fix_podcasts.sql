UPDATE podcasts SET type = 'Podcast' WHERE type IS NULL;
UPDATE podcast_requests SET type = 'Podcast' WHERE type IS NULL;

UPDATE podcast_requests 
SET author_info = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(author_info, '"Name":', '"name":'), '"Avatar":', '"avatar":'), '"Email":', '"email":'), '"Plan":', '"plan":'), '"UserId":', '"userId":')
WHERE author_info LIKE '%"Name":%';

UPDATE podcasts 
SET author = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(author, '"Name":', '"name":'), '"Avatar":', '"avatar":'), '"Email":', '"email":'), '"Plan":', '"plan":'), '"UserId":', '"userId":')
WHERE author LIKE '%"Name":%';
