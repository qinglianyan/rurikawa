import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TestSuiteAndJobCache } from 'src/services/test_suite_cacher';
import {
  Job,
  TestResult,
  FailedTestcaseOutput,
  unDiff,
} from 'src/models/job-items';
import { environment } from 'src/environments/environment';
import { endpoints } from 'src/environments/endpoints';

import JobIcon from '@iconify/icons-carbon/list-checked';
import ReportIcon from '@iconify/icons-carbon/report';
import { Title } from '@angular/platform-browser';
import { TitleService } from 'src/services/title_service';

@Component({
  selector: 'app-job-testcase-view',
  templateUrl: './job-testcase-view.component.html',
  styleUrls: ['./job-testcase-view.component.styl'],
})
export class JobTestcaseViewComponent implements OnInit, OnDestroy {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cleanHttpClient: HttpClient,
    private service: TestSuiteAndJobCache,
    private title: TitleService
  ) {}

  jobIcon = JobIcon;
  reportIcon = ReportIcon;

  jobId: string;
  testCaseKey: string;
  testCase?: TestResult;
  job?: Job;

  output?: FailedTestcaseOutput;

  stripSh(input: string) {
    try {
      let list = JSON.parse(input);
      if (list instanceof Array && list[0] === 'sh' && list[1] === '-c') {
        let cmd = list[2];
        return cmd;
      } else {
        return input;
      }
    } catch (e) {
      return input;
    }
  }

  get unDiff() {
    let diff = this.output?.stdoutDiff;
    if (diff === undefined) {
      return;
    }
    return unDiff(diff);
  }

  fetchTestCase() {
    this.service.getJob(this.jobId).subscribe({
      next: (job) => {
        this.job = job;
        this.testCase = job.results[this.testCaseKey];
        this.fetchTestCaseOutput();
      },
      error: (e) => {},
    });
  }

  fetchTestCaseOutput() {
    if (this.testCase === undefined) {
      this.router.navigate(['404']);
    }
    this.cleanHttpClient
      .get<FailedTestcaseOutput>(
        environment.endpointBase() +
          endpoints.file.get(this.testCase.resultFileId),
        { headers: { 'bypass-login': 'true' } }
      )
      .subscribe({
        next: (x) => {
          this.output = x;
        },
      });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe({
      next: (x) => {
        this.jobId = x.get('id');
        this.testCaseKey = x.get('case');
        this.fetchTestCase();
        this.title.setTitle(
          `${this.testCaseKey}: Job ${this.jobId} - Rurikawa`,
          'job-case'
        );
      },
    });
  }

  ngOnDestroy(): void {
    this.title.revertTitle('job-case');
  }
}
