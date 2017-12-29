
CREATE PROCEDURE SEIDR.[usp_DatamanagerTest]
    @MapName varchar(50),
    @TestOther int,
    @DefaultTest bit = 1
AS
BEGIN
    SELECT @MapName [Map], @TestOther [TestOther], @DefaultTest [DefaultTest]
END