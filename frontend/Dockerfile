# Build stage
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Create production build (for AngularJS we just copy static files)
# Since AngularJS 1.x doesn't have build step like Angular 2+, we just prepare files
RUN mkdir -p /app/dist && cp -r src/* /app/dist/

# Copy library files from node_modules to dist/libs
RUN mkdir -p /app/dist/libs/bootstrap/dist/css \
    && mkdir -p /app/dist/libs/bootstrap/dist/js \
    && mkdir -p /app/dist/libs/jquery/dist \
    && mkdir -p /app/dist/libs/angular \
    && mkdir -p /app/dist/libs/rxjs/bundles

# Copy Bootstrap
RUN cp node_modules/bootstrap/dist/css/bootstrap.min.css /app/dist/libs/bootstrap/dist/css/ \
    && cp node_modules/bootstrap/dist/js/bootstrap.bundle.min.js /app/dist/libs/bootstrap/dist/js/

# Copy jQuery
RUN cp node_modules/jquery/dist/jquery.min.js /app/dist/libs/jquery/dist/

# Copy AngularJS
RUN cp node_modules/angular/angular.min.js /app/dist/libs/angular/

# Copy RxJS
RUN cp node_modules/rxjs/bundles/rxjs.umd.min.js /app/dist/libs/rxjs/bundles/

# Runtime stage
FROM nginx:alpine

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
