## AIChat Tools – End User Documentation

This document describes the MCP tools registered in `Satrabel.AIChat.Tools`. Tool names are the identifiers you use when calling tools from the AI; titles and descriptions are intended for human operators.

---

### Pages

#### `add-page` – Add Page
- **Purpose**: Add a new page to the current DNN portal.
- **Key behavior**: Creates a page with optional title, description, parent page, and visibility. Fails if a non‑deleted page with the same name already exists or the parent page is invalid.
- **Parameters**:
  - **`pageName`** *(string, required)*: Name of the page to create.
  - **`pageTitle`** *(string, optional)*: Display title for the page.
  - **`description`** *(string, optional)*: Page description.
  - **`parentId`** *(number, optional)*: ID of the parent page. Use `-1` for a root‑level page.
  - **`visible`** *(boolean, optional)*: Whether the page is shown in the menu; defaults to visible.
- **Return value**:
  - On success: the new page `TabID` as a string.
  - On failure: an error string such as `Error: Page name cannot be empty`, `PageCreateFailed`, `PageNotFound`, or a validation error message.

#### `update-page` – Update Page
- **Purpose**: Update properties of an existing page in the current DNN portal.
- **Key behavior**: Looks up page settings by `tabId`, applies any non‑empty values you supply, and saves changes if the current user has permission.
- **Parameters**:
  - **`tabId`** *(number, required)*: ID of the page to update.
  - **`pageName`** *(string, optional)*: New page name (leave empty to keep existing).
  - **`pageTitle`** *(string, optional)*: New page title.
  - **`description`** *(string, optional)*: New page description.
  - **`parentId`** *(number, optional)*: New parent page ID. Use `-1` to keep the current parent.
- **Return value**:
  - `"PageUpdatedMessage"` on success.
  - `"PageNotFound"`, `"MethodPermissionDenied"`, or a validation error message on failure.

#### `delete-page` – Delete Page
- **Purpose**: Delete an existing page from the current DNN portal.
- **Key behavior**: Validates the page exists, is not already deleted, and that the current user can delete it before performing the deletion.
- **Parameters**:
  - **`tabId`** *(number, required)*: ID of the page to delete.
- **Return value**:
  - `"PageDeleted"` on success.
  - Error strings such as `Error: Invalid page ID`, `PageNotFound`, `MethodPermissionDenied`, or a detailed error message on failure.

#### `get-pages` – Get Pages
- **Purpose**: List all non‑deleted pages in the current portal.
- **Key behavior**: Returns basic metadata for each page; read‑only.
- **Parameters**: None.
- **Return value**: JSON array of pages with fields:
  - `TabID`, `TabName`, `Title`, `Description`, `Url`, `ParentId`.

#### `get-page` – Get Page
- **Purpose**: Retrieve details of a single page by ID.
- **Key behavior**: Uses internal page settings API; read‑only.
- **Parameters**:
  - **`pageId`** *(number, required)*: ID of the page to retrieve.
- **Return value**:
  - On success: JSON representation of the page settings.
  - On failure: `"PageNotFound"`, an `Error: ...` message, or `Error: Invalid page ID`.

---

### Modules

#### `add-module` – Add Module
- **Purpose**: Add a module instance to a page.
- **Key behavior**: Finds the requested desktop module by name, creates a module on the specified page and pane, and initializes permissions and culture. Requires permission to manage the page.
- **Parameters**:
  - **`tabId`** *(number, required)*: ID of the page to add the module to.
  - **`moduleName`** *(string, required)*: Internal desktop module name (e.g. `"DNN_HTML"`).
  - **`paneName`** *(string, optional)*: Pane to place the module in; defaults to `"ContentPane"`.
  - **`title`** *(string, optional)*: Module title; defaults to the module definition’s friendly name.
- **Return value**:
  - On success: Text of the form `"Module added with ModuleDefID: <id>"`.
  - On failure: `"Error adding module: <message>"`.

#### `get-modules` – Get Modules
- **Purpose**: List modules on a specific page.
- **Key behavior**: Reads module metadata from the specified page; read‑only.
- **Parameters**:
  - **`tabId`** *(number, required)*: ID of the page to inspect.
- **Return value**: JSON array with, for each module:
  - `ModuleID`, `ModuleName` (desktop module name), `ModuleTitle`, `PaneName`.

#### `get-html` – Get HTML Module Content
- **Purpose**: Get the current HTML content from a DNN HTML module.
- **Key behavior**: Validates the module is of type `DNN_HTML` and returns the latest workflow content; read‑only.
- **Parameters**:
  - **`tabId`** *(number, required)*: ID of the page containing the module.
  - **`moduleId`** *(number, required)*: ID of the HTML module.
- **Return value** (JSON):
  - On success: `{ moduleId, tabId, content }`.
  - If no content exists yet: `{ content: "", moduleId }`.
  - On failure: `{ error: "<message>" }`.

#### `update-html` – Update HTML Module Content
- **Purpose**: Update the HTML content of a DNN HTML module.
- **Key behavior**: Validates the target module and page, ensures it is an HTML module, and updates the workflow content.
- **Parameters**:
  - **`tabId`** *(number, required)*: ID of the page containing the module (used for validation).
  - **`moduleId`** *(number, required)*: ID of the HTML module to update.
  - **`content`** *(string, required)*: New HTML content to store.
- **Return value** (JSON):
  - On success: `{ success: true, moduleId, message: "HTML content updated successfully." }`.
  - On failure: `{ error: "<message>" }`.

---

### Files (Portal File System)

#### `get-folders` – Get Folders
- **Purpose**: List folders in the DNN file system.
- **Key behavior**: Returns all folders or the children of a given parent folder; read‑only.
- **Parameters**:
  - **`parentFolder`** *(string, optional)*: Folder path relative to the portal root (e.g. `"Images"`). Empty or `/` lists all folders.
- **Return value**: JSON array of folders with:
  - `FolderID`, `FolderName`, `FolderPath`.

#### `get-files` – Get Files
- **Purpose**: List files in a specific DNN folder.
- **Key behavior**: Uses DNN `FolderManager`/`FileManager` to read file metadata; read‑only.
- **Parameters**:
  - **`folderPath`** *(string, optional)*: Folder path relative to the portal (e.g. `"Images/Banners"`). Empty or omitted refers to the root folder.
- **Return value**:
  - On success: JSON array with `FileId`, `FileName`, `RelativePath`, `Extension`, `Size` (KB), `LastModified`.
  - On failure: `"Error: Folder '<path>' not found"` or `"Error getting files: <message>"`.

#### `read-file` – Read File
- **Purpose**: Read the content of a file stored in the DNN file system.
- **Key behavior**: Resolves the folder and file via DNN APIs and streams the file contents; read‑only.
- **Parameters**:
  - **`path`** *(string, required)*: File path relative to the portal file system (e.g. `"Documents/guide.txt"`).
- **Return value**:
  - On success: Raw file content as text.
  - On failure: Error strings such as `Error: Folder '...' not found`, `Error: File '...' not found in folder '...'`, or `Error reading file: <message>`.

#### `write-file` – Write File
- **Purpose**: Create or update a file using DNN file system APIs.
- **Key behavior**: Writes UTF‑8 content to the specified path, creating the folder if necessary. Creates a dated backup when overwriting, and blocks sensitive extensions such as `.exe`, `.dll`, `.config`, `.asax`, `.cs`, and `web.config`.
- **Parameters**:
  - **`path`** *(string, required)*: File path relative to the portal file system.
  - **`content`** *(string, required)*: Text content to write.
- **Return value** (JSON):
  - On update: `{ Success: true, Message: "File updated successfully", FileName, RelativePath, Size, LastModified, backup }`.
  - On create: `{ Success: true, Message: "File created successfully", FileName, FolderPath, Size, LastModified }`.
  - On error: `{ Success: false, Message: "Error writing file: <message>" }` or a string error for restricted extensions.

---

### System Files (Portal System Directory)

#### `get-system-files` – Get System Files
- **Purpose**: List system files and subfolders under the portal’s system directory.
- **Key behavior**: Resolves the path under the portal’s `HomeSystemDirectory` and lists both files and subdirectories; read‑only.
- **Parameters**:
  - **`folderPath`** *(string, optional)*: Relative path under the system directory. Empty means the root system folder.
- **Return value** (JSON object):
  - `Files`: array with `FileName`, `RelativePath`, `Extension`, `Size` (KB), `LastModified`.
  - `Directories`: array with `Name`, `FullPath`, `RelativePath`, `LastModified`, `Attributes`.

#### `read-system-file` – Read System File
- **Purpose**: Read the content of a text file under the portal’s system directory.
- **Key behavior**: Resolves the path under `HomeSystemDirectory`, rejects paths whose directory portion contains dots, rejects large (>10 MB) or binary files, and returns the content for allowed text files; read‑only.
- **Parameters**:
  - **`path`** *(string, required)*: Relative path under the system directory (e.g. `"config/settings.json"`).
- **Return value**:
  - On success: Raw file content as text.
  - On failure: Error messages such as `Error: File '<path>' not found`, `Error: File size (...) exceeds the maximum allowed size (10 MB)`, `Error: Cannot read binary file '...'`, or `Error reading file: <message>`.

#### `write-system-file` – Write System File
- **Purpose**: Write a text file under the portal’s system directory.
- **Key behavior**: Resolves the path under `HomeSystemDirectory`, creates directories as needed, and blocks writing to sensitive locations or file types (e.g. `.exe`, `.dll`, `.config`, `.asax`, `.cs`, `web.config`) or paths whose directory portion contains dots.
- **Parameters**:
  - **`filePath`** *(string, required)*: Relative path under the system directory.
  - **`content`** *(string, required)*: Text content to write.
- **Return value** (JSON):
  - On success: `{ Success: true, Message: "File written successfully", FileName, FilePath, Size, LastModified }`.
  - On failure: `{ Success: false, Message: "Error writing file: <message>" }` or string errors describing security restrictions.

---

### Web / SEO

#### `get-url-html` – Get HTML of a URL
- **Purpose**: Download the raw HTML of an HTTP/HTTPS URL.
- **Key behavior**: Performs an HTTP GET with `HttpClient` and returns the HTML as a string; read‑only.
- **Parameters**:
  - **`url`** *(string, required)*: Absolute URL to fetch.
- **Return value**:
  - On success: Raw HTML string.
  - On failure: JSON object `{ error: "<message>" }`.

#### `get-url-seo` – Get SEO Information of a URL
- **Purpose**: Fetch a URL and extract SEO‑relevant information from its HTML.
- **Key behavior**: Downloads the page, parses it with AngleSharp, and returns a structured JSON report with metadata, headings, links, structured data, and basic page‑speed hints; read‑only.
- **Parameters**:
  - **`url`** *(string, required)*: Absolute URL to analyze.
- **Return value** (JSON object) including fields such as:
  - `Url`, `Title`, `MetaDescription`, `MetaKeywords`, `MetaRobots`, `Language`, `Charset`, `Canonical`, `Viewport`.
  - `HreflangTags`, `OpenGraph`, `TwitterCard`.
  - `H1`, `H2`, `H3` (lists of heading texts).
  - `ImageCount`, `ImagesWithoutAlt`, `ImagesWithEmptyAlt`.
  - `InternalLinkCount`, `ExternalLinkCount`, `ExternalLinks`.
  - `StructuredData` (JSON‑LD blocks).
  - `InlineStyleCount`, `ExternalScriptCount`, `ExternalStylesheetCount`.
  - On error: `{ error: "<message>" }`.

---

### Communication

#### `send-email` – Send Email
- **Purpose**: Send an email using the portal’s configured DotNetNuke mail settings.
- **Key behavior**: Uses the host email as sender and supports plain‑text or HTML body.
- **Parameters**:
  - **`toEmail`** *(string, required)*: Recipient email address.
  - **`subject`** *(string, required)*: Email subject line.
  - **`body`** *(string, required)*: Email body content.
  - **`isHtml`** *(boolean, optional)*: Set to `true` if the body is HTML; default is `false`.
- **Return value** (JSON):
  - On success: `{ success: true, message: "Email sent successfully" }`.
  - On failure: `{ success: false, error: "<message or SMTP error>" }`.

### AIChat MCP Tools

This document describes the MCP tools registered in `AIChat/Tools`.  
All tools implement `IMcpProvider` and are registered via `ToolsExtensions.RegisterTools`.

Each tool exposes:
- **Name**: identifier used when calling the tool.
- **Title** / **Description**: human-readable explanation.
- **Parameters**: input fields with type, requirement, and description.
- **ReadOnly**: whether the tool only reads data or can change the portal or file system.

---

## Page tools

These tools manage DNN pages (tabs).

### `add-page`

- **Title**: Add Page  
- **Description**: Add a new page to the DNN portal.  
- **ReadOnly**: false (creates a new page)

**Parameters**

| Name        | Type    | Required | Description                                                         |
|------------|---------|----------|---------------------------------------------------------------------|
| `pageName` | string  | yes      | Name of the page to create (must be unique and non-empty).         |
| `pageTitle`| string  | no       | Page title; defaults to internal defaults if omitted.              |
| `description` | string | no    | Page description / meta description.                                |
| `parentId` | number  | no       | ID of parent page; `-1` means root level.                          |
| `visible`  | boolean | no       | Whether the page is visible in the menu (default: `true`).         |

**Notes**
- Fails if a non-deleted page with the same `pageName` already exists.
- Validates `parentId` when provided.
- Returns the new `TabID` as a string on success or an error message token.

---

### `update-page`

- **Title**: Update Page  
- **Description**: Update an existing page in the DNN portal.  
- **ReadOnly**: false (updates existing page)

**Parameters**

| Name         | Type    | Required | Description                                                              |
|-------------|---------|----------|--------------------------------------------------------------------------|
| `tabId`     | number  | yes      | ID of the page to update.                                               |
| `pageName`  | string  | no       | New page name; leave empty to keep current.                             |
| `pageTitle` | string  | no       | New page title; leave empty to keep current.                            |
| `description` | string| no       | New description; leave empty to keep current.                            |
| `parentId`  | number  | no       | New parent page ID; `-1` keeps the current parent.                      |

**Notes**
- Loads current page settings and applies only provided changes.
- Checks `SecurityService.CanSavePageDetails`; returns "MethodPermissionDenied" if not allowed.

---

### `delete-page`

- **Title**: Delete Page  
- **Description**: Delete an existing page from the DNN portal.  
- **ReadOnly**: false (deletes a page)

**Parameters**

| Name    | Type   | Required | Description                         |
|---------|--------|----------|-------------------------------------|
| `tabId` | number | yes      | ID of the page to delete.          |

**Notes**
- Validates that the page exists and is not already deleted.
- Checks `SecurityService.CanDeletePage`; returns "MethodPermissionDenied" if not allowed.
- Returns `"PageDeleted"`, `"PageNotFound"`, or an error message.

---

### `get-page`

- **Title**: Get Page  
- **Description**: Get details of an existing page from the DNN portal.  
- **ReadOnly**: true

**Parameters**

| Name     | Type   | Required | Description                      |
|----------|--------|----------|----------------------------------|
| `pageId` | number | yes      | ID of the page to retrieve.     |

**Notes**
- Returns serialized `PageSettings` or an error token such as `"PageNotFound"`.
- Static helper `GetPageByName` (not a tool) can retrieve by page name.

---

### `get-pages`

- **Title**: Get Pages  
- **Description**: Get list of pages of the website.  
- **ReadOnly**: true

**Parameters**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| —    | —    | —        | No parameters. |

**Notes**
- Returns JSON list of non-deleted tabs for the current portal, including `TabID`, `TabName`, `Title`, `Description`, `Url`, and `ParentId`.

---

## Module tools

These tools list modules on pages and add or manipulate specific modules.

### `add-module`

- **Title**: Add Module  
- **Description**: Add a module to a DNN page.  
- **ReadOnly**: false (creates a module instance)

**Parameters**

| Name        | Type   | Required | Description                                                              |
|-------------|--------|----------|--------------------------------------------------------------------------|
| `tabId`     | number | yes      | ID of the page to add the module to.                                    |
| `moduleName`| string | yes      | Desktop module name (e.g. `DNN_HTML`); must exist in the portal.        |
| `paneName`  | string | no       | Pane name on the page; defaults to `"ContentPane"`.                      |
| `title`     | string | no       | Module title; defaults to module definition friendly name if omitted.    |

**Notes**
- Requires permission to manage the target page.
- Returns a message including the new ModuleID.

---

### `get-modules`

- **Title**: Get Modules  
- **Description**: Get list of modules on a page.  
- **ReadOnly**: true

**Parameters**

| Name    | Type   | Required | Description                                 |
|---------|--------|----------|---------------------------------------------|
| `tabId` | number | yes      | ID of the page to get modules from.        |

**Notes**
- Returns JSON list with `ModuleID`, `ModuleName`, `ModuleTitle`, and `PaneName` for modules on the given page.

---

## File system tools (portal files)

These tools work with DNN-managed files and folders (via `FileManager`/`FolderManager`).

### `get-folders`

- **Title**: Get Folders  
- **Description**: Get list of folders of the website.  
- **ReadOnly**: true

**Parameters**

| Name          | Type   | Required | Description                                                         |
|---------------|--------|----------|---------------------------------------------------------------------|
| `parentFolder`| string | no       | Parent folder path to list children from; empty for all folders.   |

**Notes**
- Returns JSON with folder IDs, names, and paths.

---

### `get-files`

- **Title**: Get Files  
- **Description**: Get list of files from a specific folder.  
- **ReadOnly**: true

**Parameters**

| Name        | Type   | Required | Description                                                          |
|-------------|--------|----------|----------------------------------------------------------------------|
| `folderPath`| string | no       | Folder path; empty or omitted targets the root folder.              |

**Notes**
- Returns JSON list of files (`FileId`, `FileName`, `RelativePath`, `Extension`, `Size` in KB, `LastModified`).

---

### `read-file`

- **Title**: Read File  
- **Description**: Read content of a file from a specified path using the DNN file system.  
- **ReadOnly**: true

**Parameters**

| Name   | Type   | Required | Description                                  |
|--------|--------|----------|----------------------------------------------|
| `path` | string | yes      | DNN file path (`folder/filename.ext`).      |

**Notes**
- Resolves folder and file via `FolderManager`/`FileManager`.
- Returns the file contents as text or an error message.

---

### `write-file`

- **Title**: Write File  
- **Description**: Write content to a file using DNN file system APIs.  
- **ReadOnly**: false (creates or updates files)

**Parameters**

| Name    | Type   | Required | Description                                      |
|---------|--------|----------|--------------------------------------------------|
| `path`  | string | yes      | File path within the portal (folder + name).    |
| `content` | string | yes    | Text content to write.                           |

**Notes**
- Creates missing folders as needed.
- Security checks block dangerous extensions (e.g. `.exe`, `.dll`, `.config`, `.asax`, `.cs`, `web.config`).
- If the file exists, a dated backup copy is created before overwriting.
- Returns JSON describing success, size, and backup path if applicable.

---

## System file tools (portal system directory)

These tools access files under the portal's system directory (outside the DNN file system).

### `get-system-files`

- **Title**: Get System Files  
- **Description**: Get list of files and subdirectories from a specific portal system folder.  
- **ReadOnly**: true

**Parameters**

| Name        | Type   | Required | Description                                                            |
|-------------|--------|----------|------------------------------------------------------------------------|
| `folderPath`| string | no       | Path under the portal system directory; empty gives the root folder.  |

**Notes**
- Resolves the effective path using `PortalSettings.Current.HomeSystemDirectory`.
- Returns JSON with `Files` (name, relative path, extension, size, last modified) and `Directories` (name, relative path, last modified, attributes).

---

### `read-system-file`

- **Title**: Read System File  
- **Description**: Read content of a portal system file.  
- **ReadOnly**: true

**Parameters**

| Name   | Type   | Required | Description                                              |
|--------|--------|----------|----------------------------------------------------------|
| `path` | string | yes      | Relative path under the portal system directory.        |

**Notes**
- Rejects paths whose directory name contains a dot as a safety check.
- Maps to a physical path under `HomeSystemDirectory` and reads the file if it exists.
- Rejects files larger than 10 MB or detected as binary.

---

### `write-system-file`

- **Title**: Write System File  
- **Description**: Write content to a portal system file.  
- **ReadOnly**: false (creates/modifies system files)

**Parameters**

| Name      | Type   | Required | Description                                            |
|-----------|--------|----------|--------------------------------------------------------|
| `filePath`| string | yes      | Relative path under the portal system directory.      |
| `content` | string | yes      | Text content to write.                                 |

**Notes**
- Rejects empty paths and paths whose directory contains a dot.
- Creates missing directories where needed.
- Blocks writing dangerous extensions and `web.config`.
- Returns JSON with success flag and basic file info.

---

## HTML module tools

These tools work specifically with DNN HTML modules.

### `get-html`

- **Title**: Get HTML Module Content  
- **Description**: Get the HTML content from a DNN HTML module.  
- **ReadOnly**: true

**Parameters**

| Name      | Type   | Required | Description                                  |
|-----------|--------|----------|----------------------------------------------|
| `tabId`   | number | yes      | ID of the page containing the module.       |
| `moduleId`| number | yes      | ID of the HTML module.                      |

**Notes**
- Validates that the module exists and is of type `DNN_HTML`.
- Returns JSON containing `moduleId`, `tabId`, and `content`, or an error message.

---

### `update-html`

- **Title**: Update HTML Module Content  
- **Description**: Update the HTML content of a DNN HTML module by module ID.  
- **ReadOnly**: false (updates HTML content)

**Parameters**

| Name      | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `tabId`   | number | yes      | ID of the page containing the module (for validation). |
| `moduleId`| number | yes      | ID of the HTML module to update.                    |
| `content` | string | yes      | New HTML content to set.                            |

**Notes**
- Validates that the module exists and is an HTML module.
- Uses DNN workflows and `HtmlTextController` to update the top HTML version.
- Returns JSON with `success`, `moduleId`, and a message, or error JSON.

---

## URL and SEO tools

These tools fetch and analyze content from external URLs.

### `get-url-html`

- **Title**: Get HTML of an URL  
- **Description**: Retrieve raw HTML content from a specified URL.  
- **ReadOnly**: true

**Parameters**

| Name | Type   | Required | Description                           |
|------|--------|----------|---------------------------------------|
| `url`| string | yes      | Absolute URL to fetch HTML content.  |

**Notes**
- Returns the HTML string or a JSON error object.

---

### `get-url-seo`

- **Title**: Get SEO Information of an URL  
- **Description**: Retrieve and parse the HTML of a URL and extract SEO-relevant information.  
- **ReadOnly**: true

**Parameters**

| Name | Type   | Required | Description                                   |
|------|--------|----------|-----------------------------------------------|
| `url`| string | yes      | URL to retrieve and analyze for SEO.         |

**Notes**
- Uses AngleSharp to parse HTML.
- Returns JSON including title, meta tags, canonical URL, hreflang tags, Open Graph and Twitter Card data, headings, image statistics, internal/external link counts, structured data blocks, and basic page speed hints (inline styles, external scripts/stylesheets).

---

## Email tools

### `send-email`

- **Title**: Send Email  
- **Description**: Send an email using DotNetNuke mail services.  
- **ReadOnly**: false (sends outbound email)

**Parameters**

| Name      | Type    | Required | Description                                         |
|-----------|---------|----------|-----------------------------------------------------|
| `toEmail` | string  | yes      | Destination email address.                         |
| `subject` | string  | yes      | Subject line.                                      |
| `body`    | string  | yes      | Email body.                                        |
| `isHtml`  | boolean | no       | Whether the body is HTML; defaults to `false`.     |

**Notes**
- Uses portal host email and settings to send mail.
- Returns JSON with `success` flag and an optional error or success message.

