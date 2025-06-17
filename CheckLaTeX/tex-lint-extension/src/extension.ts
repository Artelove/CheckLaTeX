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
    autoAnalyzeScope: 'current' | 'project';
    autoAnalyzeDelay: number;
    autoAnalyzeShowNotification: boolean;
    periodicCheck: boolean;
    periodicCheckInterval: number;
    periodicCheckScope: 'current' | 'project';
    periodicCheckOnlyWhenActive: boolean;
    excludePatterns: string[];
}

let outputChannel: vscode.OutputChannel;
let statusBarItem: vscode.StatusBarItem;
let resultsProvider: LatexResultsProvider;
let diagnosticCollection: vscode.DiagnosticCollection;
let periodicCheckTimer: NodeJS.Timeout | null = null;
let isPeriodicCheckRunning = false;
let lastPeriodicCheck = 0;
let autoAnalyzeTimeouts: Map<string, NodeJS.Timeout> = new Map();

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
        }),
        vscode.commands.registerCommand('checklatex.togglePeriodicCheck', togglePeriodicCheck),
        vscode.commands.registerCommand('checklatex.toggleAutoAnalyze', toggleAutoAnalyze),
        vscode.commands.registerCommand('checklatex.analyzeProjectFull', analyzeProjectFull),
        vscode.commands.registerCommand('checklatex.configureAutoCheckInterval', configureAutoCheckInterval),
        vscode.commands.registerCommand('checklatex.setQuickInterval', setQuickInterval)
    ];

    // Улучшенный автоматический анализ при сохранении
    const onSaveWatcher = vscode.workspace.onDidSaveTextDocument(async (document) => {
        if (document.languageId === 'latex') {
            await handleAutoAnalyzeOnSave(document);
        }
    });

    // Слушаем изменения конфигурации для обновления периодической проверки
    const onConfigurationChanged = vscode.workspace.onDidChangeConfiguration((e) => {
        if (e.affectsConfiguration('checklatex.periodicCheck') || 
            e.affectsConfiguration('checklatex.periodicCheckInterval')) {
            setupPeriodicCheck();
        }
    });

    // Слушаем изменения активного окна для управления периодической проверкой
    const onWindowStateChanged = vscode.window.onDidChangeWindowState((e) => {
        if (getConfig().periodicCheckOnlyWhenActive) {
            if (e.focused) {
                setupPeriodicCheck();
            } else {
                stopPeriodicCheck();
            }
        }
    });

    // Инициализируем периодическую проверку
    setupPeriodicCheck();

    context.subscriptions.push(
        outputChannel,
        statusBarItem,
        diagnosticCollection,
        onSaveWatcher,
        onConfigurationChanged,
        onWindowStateChanged,
        ...commands
    );
}

export function deactivate() {
    stopPeriodicCheck();
    clearAutoAnalyzeTimeouts();
    
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
        autoAnalyzeScope: config.get('autoAnalyzeScope', 'current'),
        autoAnalyzeDelay: config.get('autoAnalyzeDelay', 1000),
        autoAnalyzeShowNotification: config.get('autoAnalyzeShowNotification', true),
        periodicCheck: config.get('periodicCheck', false),
        periodicCheckInterval: config.get('periodicCheckInterval', 30000),
        periodicCheckScope: config.get('periodicCheckScope', 'current'),
        periodicCheckOnlyWhenActive: config.get('periodicCheckOnlyWhenActive', true),
        excludePatterns: config.get('excludePatterns', [
            '**/node_modules/**',
            '**/build/**',
            '**/dist/**',
            '**/.git/**'
        ])
    };
}

function setupPeriodicCheck() {
    const config = getConfig();
    
    // Останавливаем существующий таймер
    stopPeriodicCheck();
    
    // Проверяем, нужно ли запускать периодическую проверку
    if (!config.periodicCheck) {
        updateStatusBarForPeriodicCheck(false);
        return;
    }

    // Проверяем, активно ли окно (если это требуется)
    if (config.periodicCheckOnlyWhenActive && !vscode.window.state.focused) {
        return;
    }

    // Проверяем наличие LaTeX файлов в workspace
    if (!vscode.workspace.workspaceFolders) {
        return;
    }

    // Запускаем периодическую проверку
    periodicCheckTimer = setInterval(async () => {
        await performPeriodicCheck();
    }, config.periodicCheckInterval);

    updateStatusBarForPeriodicCheck(true);
    
    outputChannel.appendLine(`=== Периодическая проверка включена ===`);
    outputChannel.appendLine(`Интервал: ${config.periodicCheckInterval / 1000} секунд`);
    outputChannel.appendLine(`Область: ${config.periodicCheckScope === 'current' ? 'текущий файл' : 'весь проект'}`);
    outputChannel.appendLine(`Только при активном окне: ${config.periodicCheckOnlyWhenActive ? 'да' : 'нет'}`);
}

function stopPeriodicCheck() {
    if (periodicCheckTimer) {
        clearInterval(periodicCheckTimer);
        periodicCheckTimer = null;
        updateStatusBarForPeriodicCheck(false);
        outputChannel.appendLine('=== Периодическая проверка остановлена ===');
    }
}

function updateStatusBarForPeriodicCheck(enabled: boolean) {
    const config = getConfig();
    
    if (enabled) {
        const periodicInterval = config.periodicCheckInterval / 1000;
        const autoDelay = config.autoAnalyzeDelay / 1000;
        
        statusBarItem.text = '$(clock) CheckLaTeX';
        statusBarItem.tooltip = `CheckLaTeX - периодическая проверка активна\n` +
            `Интервал: ${periodicInterval}сек\n` +
            `Задержка автоанализа: ${autoDelay}сек\n` +
            `Ctrl+Alt+Shift+T - быстрая настройка`;
    } else {
        const autoDelay = config.autoAnalyzeDelay / 1000;
        const autoAnalyzeEnabled = config.autoAnalyze;
        
        statusBarItem.text = '$(search) CheckLaTeX';
        let tooltip = 'Анализировать LaTeX проект\n' +
            `Ctrl+Alt+Shift+S - полный анализ\n` +
            `Ctrl+Alt+Shift+T - настройка интервалов`;
            
        if (autoAnalyzeEnabled) {
            tooltip += `\nАвтоанализ: включен (${autoDelay}сек)`;
        }
        
        statusBarItem.tooltip = tooltip;
    }
}

async function performPeriodicCheck() {
    // Предотвращаем одновременное выполнение нескольких проверок
    if (isPeriodicCheckRunning) {
        return;
    }

    // Проверяем минимальный интервал между проверками (30 секунд)
    const now = Date.now();
    if (now - lastPeriodicCheck < 30000) {
        return;
    }

    const config = getConfig();
    
    try {
        isPeriodicCheckRunning = true;
        lastPeriodicCheck = now;
        
        outputChannel.appendLine(`=== Периодическая проверка (${new Date().toLocaleTimeString()}) ===`);
        
        if (config.periodicCheckScope === 'current') {
            // Проверяем только текущий активный файл
            const activeEditor = vscode.window.activeTextEditor;
            if (activeEditor && activeEditor.document.languageId === 'latex') {
                await analyzeCurrentFileQuiet();
            }
        } else {
            // Проверяем весь проект
            await analyzeProjectQuiet();
        }
        
        outputChannel.appendLine('=== Периодическая проверка завершена ===');
        
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`ОШИБКА периодической проверки: ${errorMessage}`);
        
        // При ошибке показываем уведомление только в debug режиме
        const isDevelopment = process.env.NODE_ENV === 'development';
        if (isDevelopment) {
            vscode.window.showWarningMessage(`CheckLaTeX периодическая проверка: ${errorMessage}`);
        }
    } finally {
        isPeriodicCheckRunning = false;
    }
}

async function togglePeriodicCheck() {
    const config = getConfig();
    const newValue = !config.periodicCheck;
    
    await vscode.workspace.getConfiguration('checklatex').update('periodicCheck', newValue, vscode.ConfigurationTarget.Workspace);
    
    if (newValue) {
        vscode.window.showInformationMessage('CheckLaTeX: Периодическая проверка включена');
    } else {
        vscode.window.showInformationMessage('CheckLaTeX: Периодическая проверка отключена');
    }
}

// "Тихие" версии функций анализа для периодической проверки
async function analyzeProjectQuiet() {
    if (!vscode.workspace.workspaceFolders) {
        return;
    }

    const workspaceFolder = vscode.workspace.workspaceFolders[0];
    const config = getConfig();
    
    try {
        // Создаем ZIP архив проекта
        const zipBuffer = await createProjectZip(workspaceFolder.uri.fsPath, config.excludePatterns);
        
        // Отправляем на сервер
        const result = await sendZipToServer(zipBuffer, 'project.zip', config);
        
        // Обрабатываем результат
        await processAnalysisResult(result, workspaceFolder);
        
    } catch (error) {
        // Логируем ошибку, но не показываем пользователю
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`Ошибка периодической проверки проекта: ${errorMessage}`);
        throw error;
    }
}

async function analyzeCurrentFileQuiet() {
    const activeEditor = vscode.window.activeTextEditor;
    if (!activeEditor || activeEditor.document.languageId !== 'latex') {
        return;
    }

    const config = getConfig();
    const filePath = activeEditor.document.uri.fsPath;
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(activeEditor.document.uri);
    
    if (!workspaceFolder) {
        return;
    }

    try {
        // Создаем ZIP с текущим файлом и зависимостями
        const zipBuffer = await createFileZip(filePath, workspaceFolder.uri.fsPath, config.excludePatterns);
        
        // Определяем стартовый файл
        const startFile = path.relative(workspaceFolder.uri.fsPath, filePath);
        
        // Отправляем на сервер
        const result = await sendZipToServer(zipBuffer, 'current-file.zip', config, startFile);
        
        // Обрабатываем результат
        await processAnalysisResult(result, workspaceFolder);
        
    } catch (error) {
        // Логируем ошибку, но не показываем пользователю
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`Ошибка периодической проверки файла: ${errorMessage}`);
        throw error;
    }
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
                    const endCol = error.endColumnNumber ? Math.max(0, error.endColumnNumber - 1) : startCol + 5;
                    console.log(line, startCol, endLine, endCol);
                    console.log(error.fileName, error.lineNumber, error.columnNumber, error.endLineNumber, error.endColumnNumber);
                    
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

async function handleAutoAnalyzeOnSave(document: vscode.TextDocument) {
    const config = getConfig();
    
    if (!config.autoAnalyze) {
        return;
    }

    const filePath = document.uri.fsPath;
    const fileKey = document.uri.toString();

    // Отменяем предыдущий таймер для этого файла, если он есть
    const existingTimeout = autoAnalyzeTimeouts.get(fileKey);
    if (existingTimeout) {
        clearTimeout(existingTimeout);
    }

    // Устанавливаем новый таймер с задержкой
    const timeout = setTimeout(async () => {
        try {
            outputChannel.appendLine(`=== Автоматический анализ при сохранении (${new Date().toLocaleTimeString()}) ===`);
            outputChannel.appendLine(`Файл: ${filePath}`);
            outputChannel.appendLine(`Область: ${config.autoAnalyzeScope === 'current' ? 'текущий файл' : 'весь проект'}`);

            if (config.autoAnalyzeScope === 'current') {
                await analyzeCurrentFileWithNotification(config.autoAnalyzeShowNotification);
            } else {
                await analyzeProjectWithNotification(config.autoAnalyzeShowNotification);
            }

            outputChannel.appendLine('=== Автоматический анализ завершен ===');
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
            outputChannel.appendLine(`ОШИБКА автоматического анализа: ${errorMessage}`);
            
            if (config.autoAnalyzeShowNotification) {
                vscode.window.showErrorMessage(`CheckLaTeX: Ошибка автоматического анализа: ${errorMessage}`);
            }
        } finally {
            // Удаляем таймер из Map
            autoAnalyzeTimeouts.delete(fileKey);
        }
    }, config.autoAnalyzeDelay);

    // Сохраняем таймер в Map
    autoAnalyzeTimeouts.set(fileKey, timeout);
}

function clearAutoAnalyzeTimeouts() {
    for (const timeout of autoAnalyzeTimeouts.values()) {
        clearTimeout(timeout);
    }
    autoAnalyzeTimeouts.clear();
}

async function toggleAutoAnalyze() {
    const config = getConfig();
    const newValue = !config.autoAnalyze;
    
    await vscode.workspace.getConfiguration('checklatex').update('autoAnalyze', newValue, vscode.ConfigurationTarget.Workspace);
    
    if (newValue) {
        vscode.window.showInformationMessage('CheckLaTeX: Автоматический анализ при сохранении включен');
    } else {
        vscode.window.showInformationMessage('CheckLaTeX: Автоматический анализ при сохранении отключен');
        // Очищаем все отложенные таймеры
        clearAutoAnalyzeTimeouts();
    }
}

// Версии функций анализа с настраиваемыми уведомлениями
async function analyzeCurrentFileWithNotification(showNotification: boolean = true) {
    const activeEditor = vscode.window.activeTextEditor;
    if (!activeEditor || activeEditor.document.languageId !== 'latex') {
        if (showNotification) {
            vscode.window.showErrorMessage('Откройте .tex файл для анализа');
        }
        return;
    }

    const config = getConfig();
    const filePath = activeEditor.document.uri.fsPath;
    const workspaceFolder = vscode.workspace.getWorkspaceFolder(activeEditor.document.uri);
    
    if (!workspaceFolder) {
        if (showNotification) {
            vscode.window.showErrorMessage('Файл должен быть в открытой рабочей области');
        }
        return;
    }

    try {
        if (showNotification) {
            statusBarItem.text = '$(loading~spin) Анализирую файл...';
        }
        
        // Создаем ZIP с текущим файлом и зависимостями
        const zipBuffer = await createFileZip(filePath, workspaceFolder.uri.fsPath, config.excludePatterns);
        
        // Определяем стартовый файл
        const startFile = path.relative(workspaceFolder.uri.fsPath, filePath);
        
        // Отправляем на сервер
        const result = await sendZipToServer(zipBuffer, 'current-file.zip', config, startFile);
        
        // Обрабатываем результат
        await processAnalysisResult(result, workspaceFolder);
        
        if (showNotification) {
            statusBarItem.text = '$(check) Готово';
            vscode.window.showInformationMessage('Анализ файла завершен');
        }
        
    } catch (error) {
        if (showNotification) {
            statusBarItem.text = '$(error) Ошибка';
        }
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`ОШИБКА: ${errorMessage}`);
        if (showNotification) {
            vscode.window.showErrorMessage(`Ошибка анализа: ${errorMessage}`);
        }
        throw error;
    } finally {
        if (showNotification) {
            // Возвращаем исходный текст через 3 секунды
            setTimeout(() => {
                statusBarItem.text = '$(search) CheckLaTeX';
            }, 3000);
        }
    }
}

async function analyzeProjectWithNotification(showNotification: boolean = true) {
    if (!vscode.workspace.workspaceFolders) {
        if (showNotification) {
            vscode.window.showErrorMessage('Откройте папку с LaTeX проектом');
        }
        return;
    }

    const workspaceFolder = vscode.workspace.workspaceFolders[0];
    const config = getConfig();
    
    try {
        if (showNotification) {
            statusBarItem.text = '$(loading~spin) Анализирую...';
        }
        
        // Создаем ZIP архив проекта
        const zipBuffer = await createProjectZip(workspaceFolder.uri.fsPath, config.excludePatterns);
        
        // Отправляем на сервер
        const result = await sendZipToServer(zipBuffer, 'project.zip', config);
        
        // Обрабатываем результат
        await processAnalysisResult(result, workspaceFolder);
        
        if (showNotification) {
            statusBarItem.text = '$(check) Готово';
            vscode.window.showInformationMessage('Анализ LaTeX проекта завершен');
        }
        
    } catch (error) {
        if (showNotification) {
            statusBarItem.text = '$(error) Ошибка';
        }
        const errorMessage = error instanceof Error ? error.message : 'Неизвестная ошибка';
        outputChannel.appendLine(`ОШИБКА: ${errorMessage}`);
        if (showNotification) {
            vscode.window.showErrorMessage(`Ошибка анализа: ${errorMessage}`);
        }
        throw error;
    } finally {
        if (showNotification) {
            // Возвращаем исходный текст через 3 секунды
            setTimeout(() => {
                statusBarItem.text = '$(search) CheckLaTeX';
            }, 3000);
        }
    }
}

async function analyzeProjectFull() {
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
        outputChannel.appendLine('=== Начинаю полный анализ LaTeX проекта ===');
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

async function configureAutoCheckInterval() {
    const config = getConfig();
    
    // Показываем текущие настройки
    const currentPeriodicInterval = config.periodicCheckInterval / 1000;
    const currentAutoDelay = config.autoAnalyzeDelay / 1000;
    
    const options = [
        {
            label: '$(settings) Настроить интервал периодической проверки',
            description: `Текущий: ${currentPeriodicInterval} сек`,
            action: 'periodic'
        },
        {
            label: '$(clock) Настроить задержку автоанализа',
            description: `Текущая: ${currentAutoDelay} сек`,
            action: 'auto-delay'
        },
        {
            label: '$(list-selection) Показать все настройки',
            description: 'Открыть настройки CheckLaTeX',
            action: 'show-settings'
        }
    ];
    
    const selected = await vscode.window.showQuickPick(options, {
        placeHolder: 'Выберите настройку для изменения',
        title: 'Настройка интервалов CheckLaTeX'
    });
    
    if (!selected) return;
    
    switch (selected.action) {
        case 'periodic':
            await configurePeriodicInterval();
            break;
        case 'auto-delay':
            await configureAutoAnalyzeDelay();
            break;
        case 'show-settings':
            await vscode.commands.executeCommand('workbench.action.openSettings', 'checklatex');
            break;
    }
}

async function setQuickInterval() {
    const quickPresets = [
        {
            label: '$(zap) Быстрая проверка - 30 секунд',
            description: 'Для активной разработки',
            periodicInterval: 30000,
            autoDelay: 500
        },
        {
            label: '$(clock) Умеренная проверка - 2 минуты',
            description: 'Сбалансированный режим',
            periodicInterval: 120000,
            autoDelay: 1000
        },
        {
            label: '$(history) Обычная проверка - 5 минут',
            description: 'Стандартный интервал',
            periodicInterval: 300000,
            autoDelay: 2000
        },
        {
            label: '$(watch) Редкая проверка - 15 минут',
            description: 'Для больших проектов',
            periodicInterval: 900000,
            autoDelay: 3000
        },
        {
            label: '$(settings) Пользовательская настройка',
            description: 'Настроить индивидуально',
            periodicInterval: -1,
            autoDelay: -1
        }
    ];
    
    const selected = await vscode.window.showQuickPick(quickPresets, {
        placeHolder: 'Выберите предустановленный интервал',
        title: 'Быстрая настройка интервалов'
    });
    
    if (!selected) return;
    
    if (selected.periodicInterval === -1) {
        // Пользовательская настройка
        await configureAutoCheckInterval();
        return;
    }
    
    // Применяем выбранные настройки
    const config = vscode.workspace.getConfiguration('checklatex');
    
    await Promise.all([
        config.update('periodicCheckInterval', selected.periodicInterval, vscode.ConfigurationTarget.Workspace),
        config.update('autoAnalyzeDelay', selected.autoDelay, vscode.ConfigurationTarget.Workspace)
    ]);
    
    vscode.window.showInformationMessage(
        `CheckLaTeX: Установлены интервалы - периодическая проверка: ${selected.periodicInterval / 1000}сек, задержка автоанализа: ${selected.autoDelay / 1000}сек`
    );
    
    // Обновляем периодическую проверку с новыми настройками
    setupPeriodicCheck();
}

async function configurePeriodicInterval() {
    const config = getConfig();
    const currentInterval = config.periodicCheckInterval / 1000;
    
    const input = await vscode.window.showInputBox({
        prompt: 'Введите интервал периодической проверки в секундах',
        value: currentInterval.toString(),
        validateInput: (value) => {
            const num = parseInt(value);
            if (isNaN(num) || num < 2 || num > 3600) {
                return 'Интервал должен быть от 2 до 3600 секунд';
            }
            return null;
        },
        title: 'Настройка интервала периодической проверки'
    });
    
    if (!input) return;
    
    const intervalMs = parseInt(input) * 1000;
    await vscode.workspace.getConfiguration('checklatex').update(
        'periodicCheckInterval', 
        intervalMs, 
        vscode.ConfigurationTarget.Workspace
    );
    
    vscode.window.showInformationMessage(`CheckLaTeX: Интервал периодической проверки установлен на ${input} секунд`);
    setupPeriodicCheck();
}

async function configureAutoAnalyzeDelay() {
    const config = getConfig();
    const currentDelay = config.autoAnalyzeDelay / 1000;
    
    const input = await vscode.window.showInputBox({
        prompt: 'Введите задержку автоанализа в секундах',
        value: currentDelay.toString(),
        validateInput: (value) => {
            const num = parseFloat(value);
            if (isNaN(num) || num < 0 || num > 10) {
                return 'Задержка должна быть от 0 до 10 секунд';
            }
            return null;
        },
        title: 'Настройка задержки автоанализа'
    });
    
    if (!input) return;
    
    const delayMs = parseFloat(input) * 1000;
    await vscode.workspace.getConfiguration('checklatex').update(
        'autoAnalyzeDelay', 
        delayMs, 
        vscode.ConfigurationTarget.Workspace
    );
    
    vscode.window.showInformationMessage(`CheckLaTeX: Задержка автоанализа установлена на ${input} секунд`);
} 