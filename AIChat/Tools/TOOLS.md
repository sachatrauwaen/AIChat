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

