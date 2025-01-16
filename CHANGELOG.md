# Changelog (Release Notes)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 2.0.0

### Changed

- Introduces IClaimsProvider interface to allow for claims to be injected into DI container and later used classes in the call chain 

## 1.3.0

### Changed

- If no [FunctionAuthorize] attribute is present, the function will be authorized by default
- Updated nuget package dependencies

## 1.2.0

### Added

- Added support for worker defaults azure functions
- Added support for non-HTTP triggered functions

## 1.1.1

### Added

- Added PackageReleaseNotes and PackageProjectUrl

## 1.1.0

### Added

- Added support for reading claims from Bearer Token and adding them to FunctionContext
- REAMDE.md
- CHANGELOG.md

## 1.0.0

### Added

- initial release