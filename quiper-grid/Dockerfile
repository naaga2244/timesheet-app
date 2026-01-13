# Build stage
FROM node:18 AS build
WORKDIR /app
ARG REACT_APP_API_URL
ENV REACT_APP_API_URL=$REACT_APP_API_URL
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Runtime stage
FROM node:18-alpine
WORKDIR /app
RUN npm i -g serve
COPY --from=build /app/build ./build
EXPOSE 3030
CMD ["serve", "-s", "build", "-l", "3030"]
