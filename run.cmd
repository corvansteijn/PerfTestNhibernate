@echo off
pushd PerfTestNhibernate\bin\Debug
set count=50

:start

PerfTestNhibernate.exe StatefulSessionWithTransactionScopeNoComplete output.csv %count%
PerfTestNhibernate.exe StatelessSessionWithTransactionScopeNoComplete output.csv %count%

REM PerfTestNhibernate.exe StatefulSessionWithTransactionScope output.csv %count%
REM PerfTestNhibernate.exe ReadonlySessionWithTransactionScope output.csv %count%
REM PerfTestNhibernate.exe StatelessSessionWithTransactionScope output.csv %count%
REM PerfTestNhibernate.exe StatefulSessionWithFlush output.csv %count%
REM PerfTestNhibernate.exe ReadonlySessionWithFlush output.csv %count%
REM PerfTestNhibernate.exe StatelessSessionWithFlush output.csv %count%
REM PerfTestNhibernate.exe StatefulSession output.csv %count%
REM PerfTestNhibernate.exe StatefulSessionCustomTypes output.csv %count%
REM PerfTestNhibernate.exe StatefulSessionWrapResultSets output.csv %count%
REM PerfTestNhibernate.exe StatelessSession output.csv %count%
REM PerfTestNhibernate.exe SqlSession output.csv %count%
REM PerfTestNhibernate.exe ReadonlySession output.csv %count%
REM PerfTestNhibernate.exe HqlSession output.csv %count%
REM PerfTestNhibernate.exe CustomTupilizer output.csv %count%

goto start