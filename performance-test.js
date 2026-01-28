#!/usr/bin/env node

/**
 * Performance test for TenderTracker API
 * Measures response times and throughput
 */

const http = require('http');
const { performance } = require('perf_hooks');

const API_BASE_URL = 'http://localhost:5000/api';
const CONCURRENT_REQUESTS = 10;
const TOTAL_REQUESTS = 100;

async function makeRequest(url) {
  return new Promise((resolve, reject) => {
    const start = performance.now();
    const req = http.request(url, (res) => {
      let body = '';
      res.on('data', (chunk) => (body += chunk));
      res.on('end', () => {
        const end = performance.now();
        resolve({
          statusCode: res.statusCode,
          duration: end - start,
          bodyLength: body.length
        });
      });
    });

    req.on('error', reject);
    req.end();
  });
}

async function runLoadTest() {
  console.log('========================================');
  console.log('TenderTracker Performance Test');
  console.log('========================================\n');

  console.log(`Configuration:`);
  console.log(`  API Base URL: ${API_BASE_URL}`);
  console.log(`  Concurrent requests: ${CONCURRENT_REQUESTS}`);
  console.log(`  Total requests: ${TOTAL_REQUESTS}`);
  console.log('');

  const endpoints = [
    '/searchqueries',
    '/foundtenders?page=1&pageSize=10',
    '/foundtenders/stats'
  ];

  const results = {
    totalRequests: 0,
    successfulRequests: 0,
    failedRequests: 0,
    totalDuration: 0,
    durations: [],
    byEndpoint: {}
  };

  // Initialize endpoint tracking
  endpoints.forEach(endpoint => {
    results.byEndpoint[endpoint] = {
      total: 0,
      success: 0,
      fail: 0,
      durations: []
    };
  });

  // Run load test
  console.log('Running load test...\n');

  const testStart = performance.now();

  // Create batches of concurrent requests
  const batches = Math.ceil(TOTAL_REQUESTS / CONCURRENT_REQUESTS);
  
  for (let batch = 0; batch < batches; batch++) {
    const batchPromises = [];
    const batchSize = Math.min(CONCURRENT_REQUESTS, TOTAL_REQUESTS - batch * CONCURRENT_REQUESTS);

    for (let i = 0; i < batchSize; i++) {
      const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
      const url = `${API_BASE_URL}${endpoint}`;
      
      batchPromises.push(
        makeRequest(url)
          .then(result => {
            results.totalRequests++;
            results.durations.push(result.duration);
            results.totalDuration += result.duration;

            const endpointStats = results.byEndpoint[endpoint];
            endpointStats.total++;
            endpointStats.durations.push(result.duration);

            if (result.statusCode >= 200 && result.statusCode < 300) {
              results.successfulRequests++;
              endpointStats.success++;
            } else {
              results.failedRequests++;
              endpointStats.fail++;
            }
          })
          .catch(error => {
            results.totalRequests++;
            results.failedRequests++;
            console.error(`Request failed: ${error.message}`);
          })
      );
    }

    await Promise.all(batchPromises);
    process.stdout.write(`\rProgress: ${Math.min((batch + 1) * CONCURRENT_REQUESTS, TOTAL_REQUESTS)}/${TOTAL_REQUESTS} requests`);
  }

  const testEnd = performance.now();
  const totalTestTime = testEnd - testStart;

  console.log('\n\n========================================');
  console.log('Performance Test Results');
  console.log('========================================\n');

  // Calculate statistics
  const sortedDurations = results.durations.sort((a, b) => a - b);
  const avgDuration = results.totalDuration / results.totalRequests;
  const minDuration = sortedDurations[0] || 0;
  const maxDuration = sortedDurations[sortedDurations.length - 1] || 0;
  const medianDuration = sortedDurations[Math.floor(sortedDurations.length / 2)] || 0;
  const p95Duration = sortedDurations[Math.floor(sortedDurations.length * 0.95)] || 0;
  const p99Duration = sortedDurations[Math.floor(sortedDurations.length * 0.99)] || 0;

  const requestsPerSecond = results.totalRequests / (totalTestTime / 1000);

  console.log('Overall Statistics:');
  console.log(`  Total requests: ${results.totalRequests}`);
  console.log(`  Successful: ${results.successfulRequests}`);
  console.log(`  Failed: ${results.failedRequests}`);
  console.log(`  Success rate: ${((results.successfulRequests / results.totalRequests) * 100).toFixed(2)}%`);
  console.log('');
  console.log('Response Times (ms):');
  console.log(`  Average: ${avgDuration.toFixed(2)}`);
  console.log(`  Minimum: ${minDuration.toFixed(2)}`);
  console.log(`  Maximum: ${maxDuration.toFixed(2)}`);
  console.log(`  Median: ${medianDuration.toFixed(2)}`);
  console.log(`  95th percentile: ${p95Duration.toFixed(2)}`);
  console.log(`  99th percentile: ${p99Duration.toFixed(2)}`);
  console.log('');
  console.log('Throughput:');
  console.log(`  Total test time: ${(totalTestTime / 1000).toFixed(2)}s`);
  console.log(`  Requests per second: ${requestsPerSecond.toFixed(2)}`);
  console.log('');

  // Endpoint-specific statistics
  console.log('Endpoint Statistics:');
  console.log('------------------------------------------------');
  
  endpoints.forEach(endpoint => {
    const stats = results.byEndpoint[endpoint];
    if (stats.total > 0) {
      const avg = stats.durations.reduce((sum, d) => sum + d, 0) / stats.durations.length;
      const sorted = stats.durations.sort((a, b) => a - b);
      const p95 = sorted[Math.floor(sorted.length * 0.95)] || 0;
      
      console.log(`  ${endpoint}:`);
      console.log(`    Requests: ${stats.total} (${stats.success} success, ${stats.fail} fail)`);
      console.log(`    Avg response time: ${avg.toFixed(2)}ms`);
      console.log(`    95th percentile: ${p95.toFixed(2)}ms`);
      console.log('');
    }
  });

  console.log('========================================');
  console.log('Performance Test Complete');
  console.log('========================================\n');

  // Determine if performance is acceptable
  const acceptableAvgResponseTime = 100; // 100ms
  const acceptableSuccessRate = 95; // 95%
  
  const successRate = (results.successfulRequests / results.totalRequests) * 100;
  
  if (avgDuration <= acceptableAvgResponseTime && successRate >= acceptableSuccessRate) {
    console.log('✅ Performance meets acceptable standards');
    process.exit(0);
  } else {
    console.log('⚠️  Performance below acceptable standards');
    if (avgDuration > acceptableAvgResponseTime) {
      console.log(`   Average response time ${avgDuration.toFixed(2)}ms exceeds ${acceptableAvgResponseTime}ms`);
    }
    if (successRate < acceptableSuccessRate) {
      console.log(`   Success rate ${successRate.toFixed(2)}% below ${acceptableSuccessRate}%`);
    }
    process.exit(1);
  }
}

// Run performance test
runLoadTest().catch(error => {
  console.error('Performance test error:', error);
  process.exit(1);
});
