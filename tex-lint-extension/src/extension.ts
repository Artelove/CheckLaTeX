import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import axios from 'axios';
const archiver = require('archiver');
import FormData from 'form-data';

interface LatexAnalysisResult {
    commandsFound: number;
    testResults: Array<{
        testName: string;
        errors: Array<{
            type: string;
            info: string;
            command?: string;
            fileName?: string;
            lineNumber?: number;
            columnNumber?: number;
            endLineNumber?: number;
            endColumnNumber?: number;
        }>;
    }>;
    text: string;
}

interface ExtensionConfig {
    serverUrl: string;
    startFile: string;
    timeout: number;
    autoAnalyze: boolean;
    excludePatterns: string[];
}

let outputChannel: vscode.OutputChannel;
let statusBarItem: vscode.StatusBarItem;
let resultsProvider: LatexResultsProvider;
let diagnosticCollection: vscode.DiagnosticCollection;

export function activate(context: vscode.ExtensionContext) {
    console.log('CheckLaTeX Extension активировано');

    // Создаем канал вывода
    outputChannel = vscode.window.createOutputChannel('CheckLaTeX');
    
    // Создаем элемент статус-бара
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    statusBarItem.command = 'checklatex.analyzeProject';
    statusBarItem.text = '$(search) CheckLaTeX';
    statusBarItem.tooltip = 'Анализировать LaTeX проект';
    statusBarItem.show();
    
    // Создаем провайдер результатов
    resultsProvider = new LatexResultsProvider();
    vscode.window.registerTreeDataProvider('checklatexResults', resultsProvider);
    
    // Создаем коллекцию диагностик для подсветки ошибок
    diagnosticCollection = vscode.languages.createDiagnosticCollection('checklatex');
    
    // Регистрируем команды
    const commands = [
        vscode.commands.registerCommand('checklatex.analyzeProject', analyzeProject),
        vscode.commands.registerCommand('checklatex.analyzeCurrentFile', analyzeCurrentFile),
        vscode.commands.registerCommand('checklatex.configureServer', configureServer),
        vscode.commands.registerCommand('checklatex.refreshResults', () => resultsProvider.refresh()),
        vscode.commands.registerCommand('checklatex.clearDiagnostics', () => {
            diagnosticCollection.clear();
            vscode.window.showInformationMessage('CheckLaTeX: Диагностики очищены');
        })
    ];

    // Автоматический анализ при сохранении
    const onSaveWatcher = vscode.workspace.onDidSaveTextDocument((document) => {
        if (getConfig().autoAnalyze && document.languageId === 'latex') {
            analyzeCurrentFile();
        }
    });

    context.subscriptions.push(
        outputChannel,
        statusBarItem,
        diagnosticCollection,
        onSaveWatcher,
        ...commands
    );
}

export function deactivate() {
    if (outputChannel) {
        outputChannel.dispose();
    }
    if (statusBarItem) {
        statusBarItem.dispose();
    }
}

function getConfig(): ExtensionConfig {
    const config = vscode.workspace.getConfiguration('checklatex');
    return {
        serverUrl: config.get('serverUrl', 'http://localhost:5000'),
        startFile: config.get('startFile', ''),
        timeout: config.get('timeout', 30000),
        autoAnalyze: config.get('autoAnalyze', false),
        excludePatterns: config.get('excludePatterns', [
            '**/node_modules/**',
            '**/build/**',
            '**/dist/**',
            '**/.git/**'
        ])
    };
}

async function analyzeProject() {
    if (!vscode.workspace.workspaceFolders) {
        vscode.window.showErrorMessage('Откройте папку с LaTeX проектом');
        return;
    }

    const workspaceFolder = vscode.workspace.workspaceFolders[0];
    const config = getConfig();
    
    try {
        statusBarItem.text = '$(loading~spin) Анализирую...';
        outputChannel.clear();
        outputChannel.show();
        outputChannel.appendLine('=== Начинаю анализ LaTeX проекта ===');
        outputChannel.appendLine(`Папка проекта: ${workspaceFolder.uri.fsPath}`);
        outputChannel.appendLine(`Сервер: ${config.serverUrl}`);
        
        // Создаем ZIP архив проекта
        const zipBuffer = await createProjectZip(workspaceFolder.uri.fsPath, config.excludePatterns);
        outputChannel.appendLine(`Размер ZIP архива: ${zipBuffer.length} байт`);
        
        // Отправляем на сервер
        const result = await sendZipToServer(zipBuffer, 'project.zip', config);
        
        // Обрабатываем результат
        await processAnalysisResult(result, workspaceFolder);
        
        statusBarItem.text = '$(check) Готово';
        vscode.window.showInformationMessage('Анализ LaTeX проекта завершен');
        
    } catch (error) {
        statusBarItem.text = '$(error) Ошибка';
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`ОШИБКА: ${errorMessage}`);
        vscode.window.showErrorMessage(`Ошибка анализа: ${errorMessage}`);
    }
    
    // Возвращаем исходный текст через 3 секунды
    setTimeout(() => {
        statusBarItem.text = '$(search) CheckLaTeX';
    }, 3000);
}

async function analyzeCurrentFile() {
    const activeEditor = vscode.window.activeTextEditor;
    if (!activeEditor || activeEditor.document.languageId !== 'latex') {
        vscode.window.showErrorMessage('Откройте .tex файл для анализа');
        return;
    }

    const config = getConfig();
    const filePath = activeEditor.document.uri.fsPath;
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(activeEditor.document.uri);
    
    if (!workspaceFolder) {
        vscode.window.showErrorMessage('Файл должен быть в открытой рабочей области');
        return;
    }

    try {
        statusBarItem.text = '$(loading~spin) Анализирую файл...';
        outputChannel.clear();
        outputChannel.show();
        outputChannel.appendLine('=== Начинаю анализ текущего LaTeX файла ===');
        outputChannel.appendLine(`Файл: ${filePath}`);
        
        // Создаем ZIP с текущим файлом и зависимостями
        const zipBuffer = await createFileZip(filePath, workspaceFolder.uri.fsPath, config.excludePatterns);
        outputChannel.appendLine(`Размер ZIP архива: ${zipBuffer.length} байт`);
        
        // Определяем стартовый файл
        const startFile = path.relative(workspaceFolder.uri.fsPath, filePath);
        
        // Отправляем на сервер
        const result = await sendZipToServer(zipBuffer, 'current-file.zip', config, startFile);
        
        // Обрабатываем результат
        await processAnalysisResult(result, workspaceFolder);
        
        statusBarItem.text = '$(check) Готово';
        vscode.window.showInformationMessage('Анализ файла завершен');
        
    } catch (error) {
        statusBarItem.text = '$(error) Ошибка';
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`ОШИБКА: ${errorMessage}`);
        vscode.window.showErrorMessage(`Ошибка анализа: ${errorMessage}`);
    }
    
    // Возвращаем исходный текст через 3 секунды
    setTimeout(() => {
        statusBarItem.text = '$(search) CheckLaTeX';
    }, 3000);
}

async function configureServer() {
    const config = getConfig();
    
    const newUrl = await vscode.window.showInputBox({
        prompt: 'Введите URL сервера CheckLaTeX',
        value: config.serverUrl,
        validateInput: (value) => {
            try {
                new URL(value);
                return null;
            } catch {
                return 'Некорректный URL';
            }
        }
    });
    
    if (newUrl) {
        const workspaceConfig = vscode.workspace.getConfiguration('checklatex');
        await workspaceConfig.update('serverUrl', newUrl, vscode.ConfigurationTarget.Global);
        vscode.window.showInformationMessage(`URL сервера обновлен: ${newUrl}`);
    }
}

async function createProjectZip(projectPath: string, excludePatterns: string[]): Promise<Buffer> {
    return new Promise((resolve, reject) => {
        const archive = archiver('zip', { zlib: { level: 9 } });
        const buffers: Buffer[] = [];
        
        archive.on('data', (chunk: any) => buffers.push(chunk));
        archive.on('end', () => resolve(Buffer.concat(buffers)));
        archive.on('error', reject);
        
        // Добавляем файлы, исключая по паттернам
        archive.glob('**/*', {
            cwd: projectPath,
            ignore: excludePatterns,
            dot: false
        });
        
        archive.finalize();
    });
}

async function createFileZip(filePath: string, workspacePath: string, excludePatterns: string[]): Promise<Buffer> {
    return new Promise((resolve, reject) => {
        const archive = archiver('zip', { zlib: { level: 9 } });
        const buffers: Buffer[] = [];
        
        archive.on('data', (chunk: any) => buffers.push(chunk));
        archive.on('end', () => resolve(Buffer.concat(buffers)));
        archive.on('error', reject);
        
        // Добавляем основной файл
        const relativePath = path.relative(workspacePath, filePath);
        archive.file(filePath, { name: relativePath });
        
        // Ищем и добавляем связанные файлы (input, include)
        try {
            const content = fs.readFileSync(filePath, 'utf8');
            const includes = findLatexIncludes(content, path.dirname(filePath));
            
            for (const includePath of includes) {
                if (fs.existsSync(includePath)) {
                    const relIncludePath = path.relative(workspacePath, includePath);
                    archive.file(includePath, { name: relIncludePath });
                }
            }
        } catch (error) {
            outputChannel.appendLine(`Предупреждение: не удалось найти зависимости: ${error}`);
        }
        
        archive.finalize();
    });
}

function findLatexIncludes(content: string, baseDir: string): string[] {
    const includes: string[] = [];
    const patterns = [
        /\\input\s*\{([^}]+)\}/g,
        /\\include\s*\{([^}]+)\}/g,
        /\\subfile\s*\{([^}]+)\}/g,
        /\\inputminted\s*\{[^}]+\}\s*\{([^}]+)\}/g
    ];
    
    for (const pattern of patterns) {
        let match;
        while ((match = pattern.exec(content)) !== null) {
            let includePath = match[1];
            
            // Добавляем .tex если расширение не указано
            if (!path.extname(includePath)) {
                includePath += '.tex';
            }
            
            // Преобразуем относительный путь в абсолютный
            const fullPath = path.resolve(baseDir, includePath);
            includes.push(fullPath);
        }
    }
    
    return includes;
}

async function sendZipToServer(zipBuffer: Buffer, fileName: string, config: ExtensionConfig, startFile?: string): Promise<LatexAnalysisResult> {
    const formData = new FormData();
    formData.append('zipFile', zipBuffer, {
        filename: fileName,
        contentType: 'application/zip'
    });
    
    if (startFile || config.startFile) {
        formData.append('startFile', startFile || config.startFile);
    }
    
    const response = await axios.post(
        `${config.serverUrl}/api/lint/analyze-zip`,
        formData,
        {
            headers: {
                ...formData.getHeaders(),
                'Content-Type': 'multipart/form-data'
            },
            timeout: config.timeout
        }
    );
    
    return response.data;
}

async function processAnalysisResult(result: LatexAnalysisResult, workspaceFolder?: vscode.WorkspaceFolder) {
    outputChannel.appendLine('=== РЕЗУЛЬТАТЫ АНАЛИЗА ===');
    outputChannel.appendLine(`Найдено команд: ${result.commandsFound}`);
    outputChannel.appendLine('');
    
    // Очищаем предыдущие диагностики
    diagnosticCollection.clear();
    const diagnosticMap = new Map<string, vscode.Diagnostic[]>();
    
    // Выводим результаты тестов
    let hasErrors = false;
    for (const test of result.testResults) {
        if (test.errors.length > 0) {
            hasErrors = true;
            outputChannel.appendLine(`❌ ${test.testName}:`);
            for (const error of test.errors) {
                outputChannel.appendLine(`   - ${error.type}: ${error.info}`);
                if (error.command) {
                    outputChannel.appendLine(`     Команда: ${error.command}`);
                }
                
                // Создаем диагностику для подсветки в коде
                if (error.fileName && error.lineNumber !== undefined && workspaceFolder) {
                    const filePath = path.resolve(workspaceFolder.uri.fsPath, error.fileName);
                    const uri = vscode.Uri.file(filePath);
                    
                    // Определяем позицию ошибки
                    const line = Math.max(0, (error.lineNumber || 1) - 1); // VS Code использует 0-based индексы
                    const startCol = Math.max(0, (error.columnNumber || 1) - 1);
                    const endLine = error.endLineNumber ? Math.max(0, error.endLineNumber - 1) : line;
                    const endCol = error.endColumnNumber ? Math.max(0, error.endColumnNumber - 1) : startCol + 10;
                    
                    const range = new vscode.Range(line, startCol, endLine, endCol);
                    
                    // Определяем серьезность ошибки
                    let severity = vscode.DiagnosticSeverity.Warning;
                    if (error.type.toLowerCase().includes('error')) {
                        severity = vscode.DiagnosticSeverity.Error;
                    } else if (error.type.toLowerCase().includes('info')) {
                        severity = vscode.DiagnosticSeverity.Information;
                    }
                    
                    const diagnostic = new vscode.Diagnostic(
                        range,
                        `[${test.testName}] ${error.info}`,
                        severity
                    );
                    
                    diagnostic.source = 'CheckLaTeX';
                    diagnostic.code = error.type;
                    
                    // Добавляем диагностику в карту
                    const fileKey = uri.toString();
                    if (!diagnosticMap.has(fileKey)) {
                        diagnosticMap.set(fileKey, []);
                    }
                    diagnosticMap.get(fileKey)!.push(diagnostic);
                }
            }
            outputChannel.appendLine('');
        } else {
            outputChannel.appendLine(`✅ ${test.testName}: OK`);
        }
    }
    
    // Применяем все диагностики
    for (const [fileUri, diagnostics] of diagnosticMap) {
        diagnosticCollection.set(vscode.Uri.parse(fileUri), diagnostics);
    }
    
    if (!hasErrors) {
        outputChannel.appendLine('✅ Все проверки пройдены успешно!');
    } else {
        outputChannel.appendLine(`📍 Найдено ошибок для подсветки: ${diagnosticMap.size} файлов`);
    }
    
    outputChannel.appendLine('');
    outputChannel.appendLine('=== ПОДРОБНЫЙ ОТЧЕТ ===');
    outputChannel.appendLine(result.text);
    
    // Обновляем провайдер результатов
    resultsProvider.updateResults(result);
    vscode.commands.executeCommand('setContext', 'checklatex.hasResults', true);
}

// Tree Data Provider для отображения результатов
class LatexResultsProvider implements vscode.TreeDataProvider<ResultItem> {
    private _onDidChangeTreeData: vscode.EventEmitter<ResultItem | undefined | null | void> = new vscode.EventEmitter<ResultItem | undefined | null | void>();
    readonly onDidChangeTreeData: vscode.Event<ResultItem | undefined | null | void> = this._onDidChangeTreeData.event;
    
    private results: LatexAnalysisResult | null = null;
    
    refresh(): void {
        this._onDidChangeTreeData.fire();
    }
    
    updateResults(results: LatexAnalysisResult): void {
        this.results = results;
        this.refresh();
    }
    
    getTreeItem(element: ResultItem): vscode.TreeItem {
        return element;
    }
    
    getChildren(element?: ResultItem): Thenable<ResultItem[]> {
        if (!this.results) {
            return Promise.resolve([]);
        }
        
        if (!element) {
            // Root level items
            const items: ResultItem[] = [
                new ResultItem(
                    `Команд найдено: ${this.results.commandsFound}`,
                    vscode.TreeItemCollapsibleState.None,
                    'info'
                )
            ];
            
            for (const test of this.results.testResults) {
                const hasErrors = test.errors.length > 0;
                items.push(new ResultItem(
                    `${hasErrors ? '❌' : '✅'} ${test.testName}`,
                    hasErrors ? vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None,
                    hasErrors ? 'error' : 'success',
                    test
                ));
            }
            
            return Promise.resolve(items);
        } else if (element.testResult) {
            // Show test errors
            const errors = element.testResult.errors.map(error => 
                new ResultItem(
                    `${error.type}: ${error.info}`,
                    vscode.TreeItemCollapsibleState.None,
                    'error-detail'
                )
            );
            return Promise.resolve(errors);
        }
        
        return Promise.resolve([]);
    }
}

class ResultItem extends vscode.TreeItem {
    constructor(
        public readonly label: string,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        public readonly type: 'info' | 'success' | 'error' | 'error-detail',
        public readonly testResult?: { testName: string; errors: any[] }
    ) {
        super(label, collapsibleState);
        
        this.tooltip = this.label;
        
        switch (type) {
            case 'info':
                this.iconPath = new vscode.ThemeIcon('info');
                break;
            case 'success':
                this.iconPath = new vscode.ThemeIcon('check');
                break;
            case 'error':
                this.iconPath = new vscode.ThemeIcon('error');
                break;
            case 'error-detail':
                this.iconPath = new vscode.ThemeIcon('warning');
                break;
        }
    }
} 