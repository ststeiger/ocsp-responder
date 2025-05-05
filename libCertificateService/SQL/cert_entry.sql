
INSERT INTO tls_certificates.domain 
( 
	 domain_id 
	,domain_parent_domain_id 
	,domain_punycode 
	,domain_level 
	,domain_dstat_id 
	,domain_created_at 
) 
SELECT 
	 20000006 AS domain_id 
	,2 AS domain_parent_domain_id 
	,'cert.henri-bernhard.ch' AS domain_punycode 
	,1 AS domain_level 
	,0 AS domain_dstat_id 
	,CURRENT_TIMESTAMP AS domain_created_at 
WHERE NOT EXISTS( SELECT * FROM tls_certificates.domain WHERE domain_id = 20000006 ) 
; 
