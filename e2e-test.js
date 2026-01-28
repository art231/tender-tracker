#!/usr/bin/env node

/**
 * End-to-end test for TenderTracker application
 * Tests backend API and frontend data display
 */

const http = require('http');
const https = require('https');
const { exec } = require('child_process');
const util = require('util');

const execPromise = util.promisify(exec);

const API_BASE_URL = 'http://localhost:5000/api'; // Assuming backend runs on port 5000
const FRONTEND_URL = 'http://localhost:3000'; // Assuming frontend runs on port 3000

async function makeRequest(url, method = 'GET', data = null) {
  return new Promise((resolve, reject) => {
    const options = {
      method,
      headers: {
        'Content-Type': 'application/json',
      },
    };

    const req = http.request(url, options, (res) => {
      let body = '';
      res.on('data', (chunk) => (body += chunk));
      res.on('end', () => {
        try {
          const parsed = body ? JSON.parse(body) : {};
          resolve({
            statusCode: res.statusCode,
            headers: res.headers,
            body: parsed,
          });
        } catch (e) {
          resolve({
            statusCode: res.statusCode,
            headers: res.headers,
            body: body,
          });
        }
      });
    });

    req.on('error', reject);
    if (data) {
      req.write(JSON.stringify(data));
    }
    req.end();
  });
}

async function testBackendAPI() {
  console.log('Testing Backend API...\n');

  try {
    // Test 1: Get search queries
    console.log('1. Testing GET /searchqueries...');
    const queriesResponse = await makeRequest(`${API_BASE_URL}/searchqueries`);
    console.log(`   Status: ${queriesResponse.statusCode}`);
    console.log(`   Response: ${JSON.stringify(queriesResponse.body, null, 2).substring(0, 200)}...`);

    if (queriesResponse.statusCode === 200) {
      console.log('   ✓ Search queries endpoint works\n');
    } else {
      console.log('   ✗ Search queries endpoint failed\n');
      return false;
    }

    // Test 2: Get found tenders
    console.log('2. Testing GET /foundtenders...');
    const tendersResponse = await makeRequest(`${API_BASE_URL}/foundtenders?page=1&pageSize=5`);
    console.log(`   Status: ${tendersResponse.statusCode}`);
    
    if (tendersResponse.statusCode === 200) {
      console.log('   ✓ Found tenders endpoint works\n');
      
      // Check response structure
      const data = tendersResponse.body;
      if (data.tenders !== undefined && data.totalCount !== undefined) {
        console.log(`   Found ${data.totalCount} tenders total\n`);
      }
    } else {
      console.log('   ✗ Found tenders endpoint failed\n');
      return false;
    }

    // Test 3: Get stats
    console.log('3. Testing GET /foundtenders/stats...');
    const statsResponse = await makeRequest(`${API_BASE_URL}/foundtenders/stats`);
    console.log(`   Status: ${statsResponse.statusCode}`);
    
    if (statsResponse.statusCode === 200) {
      console.log('   ✓ Stats endpoint works\n');
      console.log(`   Stats: ${JSON.stringify(statsResponse.body)}\n`);
    } else {
      console.log('   ✗ Stats endpoint failed\n');
      return false;
    }

    // Test 4: Create a test search query
    console.log('4. Testing POST /searchqueries...');
    const testQuery = {
      keyword: 'e2e-test-' + Date.now(),
      category: 'Test',
      isActive: true
    };
    
    const createResponse = await makeRequest(
      `${API_BASE_URL}/searchqueries`,
      'POST',
      testQuery
    );
    console.log(`   Status: ${createResponse.statusCode}`);
    
    if (createResponse.statusCode === 201) {
      console.log('   ✓ Create search query works\n');
      
      // Clean up: delete the test query
      const queryId = createResponse.body.id;
      console.log(`   Created query ID: ${queryId}`);
      
      // Test 5: Delete the test query
      console.log('5. Testing DELETE /searchqueries/{id}...');
      const deleteResponse = await makeRequest(
        `${API_BASE_URL}/searchqueries/${queryId}`,
        'DELETE'
      );
      console.log(`   Status: ${deleteResponse.statusCode}`);
      
      if (deleteResponse.statusCode === 204) {
        console.log('   ✓ Delete search query works\n');
      } else {
        console.log('   ✗ Delete search query failed\n');
      }
    } else {
      console.log('   ✗ Create search query failed\n');
    }

    return true;
  } catch (error) {
    console.error('   ✗ API test failed with error:', error.message);
    return false;
  }
}

async function testFrontend() {
  console.log('\nTesting Frontend...\n');
  
  try {
    // Test 1: Check if frontend server is running
    console.log('1. Checking frontend server...');
    const frontendResponse = await makeRequest(`${FRONTEND_URL}/`);
    console.log(`   Status: ${frontendResponse.statusCode}`);
    
    if (frontendResponse.statusCode === 200) {
      console.log('   ✓ Frontend server is running\n');
    } else {
      console.log('   ✗ Frontend server is not responding\n');
      return false;
    }

    // Test 2: Check if main page contains expected elements
    console.log('2. Checking frontend page structure...');
    // This would require HTML parsing, but we'll do a simple check
    if (typeof frontendResponse.body === 'string') {
      const html = frontendResponse.body;
      if (html.includes('TenderTracker') || html.includes('tender-tracker')) {
        console.log('   ✓ Frontend page contains expected content\n');
      } else {
        console.log('   ✗ Frontend page missing expected content\n');
      }
    }

    return true;
  } catch (error) {
    console.error('   ✗ Frontend test failed with error:', error.message);
    return false;
  }
}

async function runTests() {
  console.log('========================================');
  console.log('TenderTracker End-to-End Tests');
  console.log('========================================\n');

  // Check if backend is running
  console.log('Checking if backend is running...');
  try {
    await makeRequest(`${API_BASE_URL}/searchqueries`);
    console.log('✓ Backend is running\n');
  } catch (error) {
    console.log('✗ Backend is not running. Please start the backend server first.');
    console.log('  Run: cd backend/TenderTracker.API && dotnet run');
    process.exit(1);
  }

  const backendPassed = await testBackendAPI();
  const frontendPassed = await testFrontend();

  console.log('\n========================================');
  console.log('Test Summary');
  console.log('========================================');
  console.log(`Backend API: ${backendPassed ? '✓ PASSED' : '✗ FAILED'}`);
  console.log(`Frontend: ${frontendPassed ? '✓ PASSED' : '✗ FAILED'}`);
  console.log('========================================\n');

  if (backendPassed && frontendPassed) {
    console.log('All tests passed! ✅');
    process.exit(0);
  } else {
    console.log('Some tests failed. ❌');
    process.exit(1);
  }
}

// Run tests
runTests().catch(error => {
  console.error('Test runner error:', error);
  process.exit(1);
});
