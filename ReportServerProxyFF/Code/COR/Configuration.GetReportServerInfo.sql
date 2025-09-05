--0.Configuration.GetReportServerInfo.sql

select 
	 substring([FC_Value], 0, charindex('/Pages/ReportViewer.aspx', [FC_Value])) as [SSRS_Link]
from
	[T_FMS_Configuration]
where
	(
		[FC_Key] = 'reportLink' 
	);