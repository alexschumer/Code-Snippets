#include "mainwindow.h"
#include "ui_mainwindow.h"

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
    , timer(new QTimer(this))  // Initialize the timer
    , currentTask(nullptr)  // No task selected initially
    , timeToWork(0.0)  // Initialize time to work to 0
{
    ui->setupUi(this);
    ui->tableWidget->setColumnCount(2);
    ui->tableWidget->setHorizontalHeaderLabels(QStringList() << "Date" << "Time Required");

    ui->tableWidget_2->setColumnCount(1);  // Task names only need one column
    ui->tableWidget_2->setHorizontalHeaderLabels(QStringList() << "Task Name");

    ui->tableWidget_Timer->setColumnCount(1);  // Task names only need one column in the Timer table
    ui->tableWidget_Timer->setHorizontalHeaderLabels(QStringList() << "Task Name");

    ui->tableWidget->setWordWrap(true);
    ui->tableWidget->resizeRowsToContents();
    ui->tableWidget->setColumnWidth(0,75);
    ui->tableWidget->setColumnWidth(1,250);

    ui->dateEdit->setDate(QDate::currentDate().addDays(1));

    // Connect the timer to the updateTimer function
    connect(timer, &QTimer::timeout, this, &MainWindow::updateTimer);

    InitComboBox();

    // Automatically load tasks on startup
    loadTasks();
}

MainWindow::~MainWindow()
{
    saveTasks();
    clearTaskList();
    delete ui;
}

void MainWindow::InitComboBox()
{
    // Initialize the range and default values for combo boxes
    for (int i = 1; i <= 10; ++i) {
        ui->oTime_comboBox->addItem(QString::number(i));
        ui->mTime_comboBox->addItem(QString::number(i));
        ui->pTime_comboBox->addItem(QString::number(i));
    }

    // Set default values (0-based index)
    ui->oTime_comboBox->setCurrentIndex(0);  // Default to 1
    ui->mTime_comboBox->setCurrentIndex(3);  // Default to 4
    ui->pTime_comboBox->setCurrentIndex(0);  // Default to 1
}

void MainWindow::on_CreateTaskButton_clicked()
{
    QString name = ui->taskName->text();
    QDate endDate = ui->dateEdit->date();

    double oTime = ui->oTime->value();
    double mTime = ui->mTime->value();
    double pTime = ui->pTime->value();

    int oWeight = ui->oTime_comboBox->currentText().toInt();
    int mWeight = ui->mTime_comboBox->currentText().toInt();
    int pWeight = ui->pTime_comboBox->currentText().toInt();

    if(oTime == 0 && pTime == 0)
    {
        mWeight = 6;
        ui->mTime_comboBox->setCurrentIndex(5);  // Default to 6
    }

    double totalTime = ((oWeight * oTime) + (mWeight * mTime) + (pWeight * pTime)) / 6;

    if(name.isEmpty())
    {
        QMessageBox::warning(this, "Input Error" ,"Please provide a name for your task before adding it.");
        return;
    }

    task* newTask = new task(name,endDate,totalTime);
    taskList.push_back(newTask);

    QMap<QDate, QList<QPair<QString, double>>> dailyAllocations = calculateDailyAllocations();
    populateTable(dailyAllocations);

    populateTaskNameList();
}

void MainWindow::populateTaskNameList()
{
    ui->tableWidget_2->clearContents();
    ui->tableWidget_Timer->clearContents();

    ui->tableWidget_2->setRowCount(taskList.size());
    ui->tableWidget_Timer->setRowCount(taskList.size());

    for (int i = 0; i < taskList.size(); ++i)
    {
        QString taskName = taskList[i]->taskName;
        ui->tableWidget_2->setItem(i, 0, new QTableWidgetItem(taskName));
        ui->tableWidget_Timer->setItem(i, 0, new QTableWidgetItem(taskName));  // Also populate the Timer list
    }
}

QMap<QDate, QList<QPair<QString, double>>> MainWindow::calculateDailyAllocations()
{
    QMap<QDate, QList<QPair<QString, double>>> dailyAllocations;
    QDate startDate = QDate::currentDate();

    for (task* obj : taskList)
    {
        int totalDays = startDate.daysTo(obj->dueDate) - 1;

        if (totalDays < 1)
            totalDays = 1;

        double hoursPerDay = obj->estimatedTimeToComplete / totalDays;  // Calculate time per day in hours

        for (int i = 0; i < totalDays; ++i)
        {
            QDate date = startDate.addDays(i);
            dailyAllocations[date].append(qMakePair(obj->taskName, hoursPerDay));
        }
    }

    return dailyAllocations;
}

void MainWindow::populateTable(const QMap<QDate, QList<QPair<QString, double>>>& dailyAllocations)
{
    ui->tableWidget->clearContents();
    ui->tableWidget->setRowCount(dailyAllocations.size());

    int row = 0;
    for (auto it = dailyAllocations.constBegin(); it != dailyAllocations.constEnd(); ++it, ++row)
    {
        QDate date = it.key();
        QString taskDetails;

        for (const auto& pair : it.value())
        {
            double hours = pair.second;
            QString timeAllocated = QString::number(hours, 'f', 3) + " hrs";  // Display time in hours with two decimal places

            taskDetails += pair.first + ": " + timeAllocated + "\n";  // Task name and time
        }

        ui->tableWidget->setItem(row, 0, new QTableWidgetItem(date.toString("MM/dd/yyyy")));
        ui->tableWidget->setItem(row, 1, new QTableWidgetItem(taskDetails.trimmed()));  // Remove trailing newline
    }
}

int MainWindow::GetMaxDays()
{
    QDate startDate = QDate::currentDate();
    int totalDays = 0;

    for(task* obj : taskList)
    {
        if(startDate.daysTo(obj->dueDate) > totalDays)
            totalDays = startDate.daysTo(obj->dueDate);
    }

    return totalDays;
}

/******************************************************************************
method:      clearTaskList
description: Clears the memory allocated in the taskList method.
param:       NONE
return:      NONE
******************************************************************************/
void MainWindow::clearTaskList()
{
    for(task* thisTask : taskList)
    {
        delete thisTask;
    }

    taskList.clear();
}

void MainWindow::on_deleteTask_clicked()
{
    int selectedRow = ui->tableWidget_2->currentRow();
    if (selectedRow == -1) {
        QMessageBox::warning(this, "Delete Task", "Please select a task to delete.");
        return;
    }

    QString taskName = ui->tableWidget_2->item(selectedRow, 0)->text();

    // Confirmation dialog before deletion
    int response = QMessageBox::question(this, "Delete Task",
                                         "Are you sure you want to delete the task: " + taskName + "?",
                                         QMessageBox::Yes | QMessageBox::No);
    if (response == QMessageBox::No) {
        return; // User canceled deletion
    }

    // Find and delete the task in the QVector
    for (int i = 0; i < taskList.size(); ++i) {
        task* thisTask = taskList[i];
        if (thisTask->taskName == taskName) {
            delete thisTask;
            taskList.remove(i);
            break;
        }
    }

    // Update the UI
    QMap<QDate, QList<QPair<QString, double>>> dailyAllocations = calculateDailyAllocations();
    populateTable(dailyAllocations);

    // Update the task name list after deletion
    populateTaskNameList();
}

void MainWindow::saveTasks()
{
   QFile saveFile(QDir::homePath() + "/tasks.json");

    if(saveFile.isOpen())
       saveFile.close();

    if (!saveFile.open(QIODevice::WriteOnly)) {
        qWarning("Couldn't open save file.");
        return;
    }

    QJsonArray taskArray;
    for (const task* t : taskList) {
        QJsonObject taskObject;
        taskObject["name"] = t->taskName;
        taskObject["dueDate"] = t->dueDate.toString(Qt::ISODate);
        taskObject["estimatedTimeToComplete"] = t->estimatedTimeToComplete;
        taskArray.append(taskObject);
    }

    QJsonDocument saveDoc(taskArray);
    saveFile.write(saveDoc.toJson());
}

void MainWindow::loadTasks()
{
    QFile loadFile(QDir::homePath() + "/tasks.json");

    if (!loadFile.open(QIODevice::ReadOnly)) {
        qWarning("Couldn't open load file.");
        return;
    }

    QByteArray saveData = loadFile.readAll();
    QJsonDocument loadDoc(QJsonDocument::fromJson(saveData));
    QJsonArray taskArray = loadDoc.array();

    clearTaskList();

    for (int i = 0; i < taskArray.size(); ++i) {
        QJsonObject taskObject = taskArray[i].toObject();
        QString name = taskObject["name"].toString();
        QDate dueDate = QDate::fromString(taskObject["dueDate"].toString(), Qt::ISODate);
        double estimatedTimeToComplete = taskObject["estimatedTimeToComplete"].toDouble();

        task* loadedTask = new task(name, dueDate, estimatedTimeToComplete);
        taskList.push_back(loadedTask);
    }

    // Update the UI
    QMap<QDate, QList<QPair<QString, double>>> dailyAllocations = calculateDailyAllocations();
    populateTable(dailyAllocations);
    populateTaskNameList();
}

void MainWindow::on_startTimer_clicked()
{
    // Get the selected row in the Timer task list
    int selectedRow = ui->tableWidget_Timer->currentRow();
    if (selectedRow == -1) {
        QMessageBox::warning(this, "Start Timer", "Please select a task to start the timer.");
        return;
    }

    QString taskName = ui->tableWidget_Timer->item(selectedRow, 0)->text();

    // Find the task in the taskList
    for (task* thisTask : taskList) {
        if (thisTask->taskName == taskName) {
            currentTask = thisTask;
            break;
        }
    }

    if (!currentTask) {
        QMessageBox::warning(this, "Start Timer", "Selected task could not be found.");
        return;
    }

    // If the timer was previously stopped, continue with the remaining time
    if (timer->isActive()) {
        timer->stop();
    }

    // Only set the time to work if the timer was not running (fresh start)
    if (timeToWork <= 0.0) {
        // Get the time to work from the spin box (in hours)
        timeToWork = ui->doubleSpinBox_Timer->value();
    }

    if (timeToWork <= 0.0) {
        QMessageBox::warning(this, "Start Timer", "Please enter a valid time to work.");
        return;
    }

    // Display the current countdown time
    updateTimerDisplay();

    // Start the timer, triggering every 1000 milliseconds (1 second)
    timer->start(1000);
}

// Function to update the timer and the task's remaining time
void MainWindow::updateTimer()
{
    if (!currentTask || timeToWork <= 0.0) {
        timer->stop();
        return;
    }

    // Decrease the work time by 1 second (convert to hours)
    timeToWork -= 1.0 / 3600.0;  // Subtract a second worth of time in hours

    // Decrease the task's estimated time to complete in hours
    currentTask->estimatedTimeToComplete -= 1.0 / 3600.0;

    // Update the timer display label
    updateTimerDisplay();

    // Check if the task is complete
    if (currentTask->estimatedTimeToComplete <= 0.0) {
        currentTask->estimatedTimeToComplete = 0.0;  // Ensure it doesn't go negative
        QMessageBox::information(this, "Task Complete", "You have completed the task: " + currentTask->taskName);
        timer->stop();
    }

    // Update the UI to reflect the remaining time
    QMap<QDate, QList<QPair<QString, double>>> dailyAllocations = calculateDailyAllocations();
    populateTable(dailyAllocations);

    // Stop the timer if the time to work is exhausted
    if (timeToWork <= 0.0) {
        timer->stop();
    }
}

// Helper function to update the timer display label
void MainWindow::updateTimerDisplay()
{
    int totalSeconds = static_cast<int>(timeToWork * 3600);  // Convert remaining time to total seconds
    int hours = totalSeconds / 3600;
    int minutes = (totalSeconds % 3600) / 60;
    int seconds = totalSeconds % 60;

    QString timeString = QString("%1:%2:%3")
                             .arg(hours, 2, 10, QChar('0'))
                             .arg(minutes, 2, 10, QChar('0'))
                             .arg(seconds, 2, 10, QChar('0'));

    ui->label_Timer->setText(timeString);  // Update the correct label
}


void MainWindow::on_buttonStopTimer_clicked()
{
    timer->stop();
}


void MainWindow::on_timer_Reset_clicked()
{
    timer->stop();
    timeToWork = ui->doubleSpinBox_Timer->value();

    updateTimerDisplay();
}

