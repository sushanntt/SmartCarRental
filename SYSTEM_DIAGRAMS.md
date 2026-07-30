# System Diagrams for Smart Car Rental System

## 1. Detailed Class Diagram
This diagram acts as the blueprint of the system, showing all the classes, their attributes (with data types), their methods (with return types), and how they relate to one another.

```mermaid
classDiagram
    direction TB

    %% Base Class for all accounts
    class SystemUser {
        +Guid Id
        +String Firstname
        +String Lastname
        +String Email
        +void Login()
        +void UpdateProfile()
    }

    %% Customer inherits from SystemUser
    class Customer {
        +String ProfilePhotoUrl
        +String CitizenshipPhotoUrl
        +String LicencePhotoUrl
        +Boolean IsProfileComplete
        +Boolean RegularCustomer
        +Boolean CanBook
        +void Register()
        +void UploadDocuments()
        +void SubmitBooking()
    }

    %% Admin inherits from SystemUser
    class Admin {
        +void ManageCars()
        +void ManageFestivals()
        +void ApproveBooking()
        +void ManageRefunds()
    }

    class Car {
        +Guid Id
        +String ModelName
        +Int ModelYear
        +Int TotalSeats
        +String EngineType
        +Int Price
        +Boolean Available
        +Boolean IsCarBooked
        +Int BookedCount
        +void AddCar()
        +void UpdateDetails()
        +void MarkAsBooked()
    }

    class CarBrand {
        +Guid Id
        +String BrandName
        +void AddBrand()
    }

    class Rent {
        +Guid Id
        +DateTime RequestDate
        +DateTime RentalDate
        +Double Amount
        +String PaymentMethod
        +Boolean Verified
        +Double CalculateFinalPrice()
    }

    class Festival {
        +Int Id
        +String Name
        +DateTime StartDate
        +DateTime EndDate
        +Decimal PriceMultiplier
        +Boolean IsActive
        +void CreateFestival()
        +Decimal GetMultiplier()
    }

    class CarDamage {
        +Guid Id
        +String DamageDescription
        +Double DamageCharge
        +Boolean DamagePaid
        +void ReportDamage()
        +void PayDamageCharge()
    }

    class Payment {
        +Guid Id
        +Int Amount
        +DateTime PaymentDate
        +String TransactionId
        +Boolean ProcessKhaltiPayment()
    }

    class Offer {
        +Guid Id
        +String OfferTitle
        +Int DiscountPercentage
        +Int Price
        +Boolean IsBooked
        +void CreateOffer()
    }

    %% Inheritance Relationships
    SystemUser <|-- Customer : Inherits
    SystemUser <|-- Admin : Inherits

    %% Other Relationships
    Customer "1" -- "0..*" Rent : makes
    Car "1" -- "0..*" Rent : is booked in
    CarBrand "1" -- "0..*" Car : categorizes
    Customer "1" -- "0..*" CarDamage : reports
    Car "1" -- "0..*" CarDamage : sustains
    Rent "1" -- "1" Payment : processed via
    Offer "1" -- "0..*" Rent : applies to
    Admin "1" -- "0..*" Rent : approves
    
    %% Dependency: Rent uses Festival to calculate the final Amount
    Rent ..> Festival : uses for pricing
```

### Method Return Types Explained:
*   **void** — The method performs an action but does not return any data back. For example, `void AddCar()` just saves the car to the database and does not give back any value.
*   **Double** — The method returns a decimal number. For example, `Double CalculateFinalPrice()` calculates and returns the final rental amount (like 4200.00).
*   **Decimal** — Similar to Double but more precise for money. `Decimal GetMultiplier()` returns the festival price multiplier (like 1.2).
*   **Boolean** — The method returns either `true` or `false`. For example, `Boolean ProcessKhaltiPayment()` returns `true` if the payment was successful and `false` if it failed.

---

## 2. Detailed Object Diagram
The Object Diagram represents a "snapshot" of the system at a specific second in time. In this scenario: Customer "Ram" is booking a "Toyota Corolla" during the "Dashain Festival". The festival multiplier increases the price, and the Admin approves the booking.

```mermaid
classDiagram
    direction LR

    class customer1~Customer~ {
        Id = "d5b94f31-8c43-41..."
        Firstname = "Ram"
        Email = "ram@gmail.com"
        IsProfileComplete = true
        RegularCustomer = false
    }

    class admin1~Admin~ {
        Id = "a2c19e54-3b22-49..."
        Firstname = "SuperAdmin"
        Email = "admin@hajurko.com"
    }
    
    class brand1~CarBrand~ {
        Id = "b9f71c42-1a33-47..."
        BrandName = "Toyota"
    }

    class car1~Car~ {
        Id = "c4d82e11-9f44-46..."
        ModelName = "Corolla"
        Price = 3500
        IsCarBooked = true
    }

    class festival1~Festival~ {
        Id = 1
        Name = "Dashain Special"
        PriceMultiplier = 1.2
        IsActive = true
    }

    class rent1~Rent~ {
        Id = "r3a56b78-4e55-48..."
        RentalDate = "2026-10-15"
        Amount = 4200
        Verified = true
    }

    class payment1~Payment~ {
        Id = "p8b45c22-2d66-41..."
        Amount = 4200
        PaymentMethod = "Khalti"
    }

    %% Links between objects
    brand1 -- car1 : categorizes
    customer1 -- rent1 : requested
    car1 -- rent1 : assigned to
    rent1 -- payment1 : paid by
    rent1 ..> festival1 : priced using
    admin1 -- rent1 : approved by
```

### Arrow Types Summary:
*   **Empty Triangle Arrow (◁——)** = Inheritance. Means "Is A" (Customer is a SystemUser).
*   **Solid Line with Numbers** = Association. Shows database relationships (1-to-many).
*   **Dashed Arrow (- - ->)** = Dependency. Means "Uses" (Rent uses Festival for pricing).
*   **Plain Solid Line (in Object Diagram)** = Instance Link. Shows data connected right now.

---

## 3. Final State Diagram
This state diagram covers the rental request lifecycle. `Login` is a precondition in the app, so the main flow focuses on profile status, payment, approval, and return.

```mermaid
stateDiagram-v2
    [*] --> LoggedOut
    LoggedOut --> LoggedIn : User authenticates
    LoggedIn --> ProfileCheck : User selects car/offer

    state ProfileCheck {
        [*] --> Check
        Check --> ProfileIncomplete : profile incomplete
        Check --> CannotBook : CanBook == false
        Check --> ReadyToRent : profile complete && can book
    }

    ProfileIncomplete --> ProfileUpdated : user completes profile
    ProfileUpdated --> ReadyToRent

    ReadyToRent --> PaymentChoice : choose payment
    PaymentChoice --> CashRequest : cash selected
    PaymentChoice --> KhaltiRequest : Khalti selected

    CashRequest --> RentCreated
    KhaltiRequest --> PaymentSuccess
    PaymentSuccess --> RentCreated

    RentCreated --> AwaitingApproval : rent saved, Verified=false
    AwaitingApproval --> Approved : admin approves
    AwaitingApproval --> Rejected : admin rejects

    Approved --> CarInUse : car is booked
    CarInUse --> Returned : customer returns car
    Returned --> CarAvailable : car freed
    CarAvailable --> [*]

    Rejected --> RefundPending : if Khalti payment
    RefundPending --> RefundCompleted
    RefundCompleted --> [*]
    CannotBook --> [*]
```

## 4. Final Sequence Diagram
This smaller, correct sequence diagram shows the real booking flow in the code:

```mermaid
sequenceDiagram
    actor Customer
    participant HomeController
    participant RentController
    participant FestivalService
    participant Database
    participant RentsController
    participant Admin
    participant EmailService

    Customer->>HomeController: Request BeforeKhalti(carId)
    HomeController->>Database: Load car and current user
    HomeController->>HomeController: check car availability, CanBook, IsProfileComplete
    alt profile incomplete
        HomeController->>RentController: redirect to Rent.Index
        RentController->>Database: load current user
        Customer->>RentController: submit profile update
        RentController->>Database: save user profile
        RentController-->>Customer: redirect to RentDescription
    end

    Customer->>RentController: open RentDescription(carId)
    RentController->>Database: load car and user
    RentController-->>Customer: show booking page

    Customer->>RentController: confirm payment
    alt cash
        RentController->>Database: create Rent(Verified=false)
        RentController->>Database: mark car IsCarBooked=true
    else Khalti
        RentController->>FestivalService: get festival multiplier(rentalStart)
        FestivalService->>Database: query active festival
        FestivalService-->>RentController: return multiplier
        RentController->>Khalti: create payment session
        Khalti-->>RentController: callback
        RentController->>Database: create Rent(Verified=false)
        RentController->>Database: mark car IsCarBooked=true
    end

    RentController-->>Customer: booking submitted
    Admin->>RentsController: view pending rents
    Admin->>RentsController: approve rent
    RentsController->>Database: update Rent.Verified=true
    RentsController->>Database: add RentalHistory
    RentsController->>EmailService: send approval email
    EmailService-->>Customer: notify approval
```

## 4. Full Sequence Diagram (admin verification added)
This is the complete booking flow including the admin verification checks performed before approval.

```mermaid
sequenceDiagram
    actor Customer
    actor Admin
    participant AccountController
    participant HomeController
    participant BookingController
    participant FestivalService
    participant PaymentGateway
    participant Database
    participant RentalApprovalController
    participant EmailService

    %% Authentication
    Customer->>AccountController: register / login
    AccountController->>Database: create/validate user
    AccountController-->>Customer: authenticated

    %% Browse & start booking
    Customer->>HomeController: open car detail / click Book
    HomeController->>Database: load Car, User
    HomeController-->>Customer: show availability & profile status

    %% Optional profile completion (pre-book)
    opt profile incomplete or documents missing
        Customer->>BookingController: open profile form / upload docs
        BookingController->>Database: save profile, set IsProfileComplete
        BookingController-->>Customer: profile now complete
    end

    %% Pre-booking checks
    Customer->>BookingController: start booking (choose rental date)
    BookingController->>Database: re-check Car.IsCarBooked, User.CanBook
    BookingController-->>Customer: show price & payment options

    %% Payment branch
    alt Cash payment
        Customer->>BookingController: confirm cash booking
        BookingController->>FestivalService: get multiplier(rentalDate)
        FestivalService->>Database: query active festival
        FestivalService-->>BookingController: multiplier
        BookingController->>Database: create Rent (Verified=false), create Payment(Cash), mark Car.IsCarBooked=true
        BookingController-->>Customer: booking saved (awaiting approval)
    else Khalti (online)
        Customer->>PaymentGateway: initiate Khalti (amount)
        PaymentGateway-->>BookingController: redirect / callback when done
        BookingController->>FestivalService: get multiplier(rentalDate)
        FestivalService->>Database: return multiplier
        BookingController->>Database: create Rent (Verified=false), create Payment(Khalti record), mark Car.IsCarBooked=true
        BookingController-->>Customer: booking saved (awaiting approval)
    end

    %% Admin review & verification (detailed checks)
    Admin->>RentalApprovalController: open Rent details
    RentalApprovalController->>Database: load Rent, User, Payment, Car, RentalHistory
    RentalApprovalController-->>Admin: show details

    Admin->>RentalApprovalController: perform verification
    RentalApprovalController->>Database: check User.IsProfileVerified, User.CanBook, Car.IsCarBooked, Payment exists & amount, check RentalHistory/conflicts
    alt all checks pass
        Admin->>RentalApprovalController: approve rent
        RentalApprovalController->>Database: set Rent.Verified=true, add RentalHistory(Status=true)
        RentalApprovalController->>EmailService: send approval email
        EmailService-->>Customer: notify approval
    else any check fails
        Admin->>RentalApprovalController: reject or request more info
        RentalApprovalController->>Database: add RentalHistory(Status=false)
        alt if Payment.Method == "Khalti"
            RentalApprovalController->>Database: create RefundableRent(Status=false)
            RentalApprovalController->>EmailService: notify refund instructions
            EmailService-->>Customer: refund notification (manual)
        end
        RentalApprovalController->>Database: set Car.IsCarBooked=false
        RentalApprovalController-->>Customer: notify rejection
    end

    %% Return and refund completion (staff completes refund)
    Customer->>RentalApprovalController: return car (end of rental)
    RentalApprovalController->>Database: mark Returned, set Car.IsCarBooked=false
    Admin->>RentalApprovalController: process refund id
    RentalApprovalController->>Database: set RefundableRent.Status=true
    RentalApprovalController->>EmailService: send refund confirmation
    EmailService-->>Customer: refund confirmed
```

## 5. Compact Sequence Diagram
When the detailed flow is too large, use this smaller version for a cleaner overview.

```mermaid
sequenceDiagram
    actor Customer
    actor Admin
    participant AccountController
    participant HomeController
    participant BookingController
    participant Database
    participant RentalApprovalController

    Customer->>AccountController: login/register
    AccountController->>Database: create/validate user
    AccountController-->>Customer: authenticated

    Customer->>HomeController: request car booking
    HomeController->>Database: load user and car
    HomeController-->>Customer: show availability & profile status

    opt profile incomplete
        Customer->>BookingController: complete profile
        BookingController->>Database: save profile
        BookingController-->>Customer: profile complete
    end

    Customer->>BookingController: confirm booking
    BookingController->>Database: save Rent(Verified=false) and mark Car.IsCarBooked=true
    BookingController-->>Customer: booking requested

    Admin->>RentalApprovalController: verify & approve
    RentalApprovalController->>Database: check profile, payment, availability
    alt all checks pass
        RentalApprovalController->>Database: set Rent.Verified=true
        RentalApprovalController-->>Customer: approval notified
    else reject
        RentalApprovalController->>Database: set Car.IsCarBooked=false
        RentalApprovalController-->>Customer: rejection notified
    end
```

## 6. Compact State Diagram
A smaller state view with the same lifecycle steps.

```mermaid
stateDiagram-v2
    [*] --> LoggedOut
    LoggedOut --> LoggedIn : login/register
    LoggedIn --> CheckBooking : selects car

    state CheckBooking {
        [*] --> Ready
        Ready --> ProfileIncomplete : missing profile
        Ready --> CannotBook : CanBook == false
        Ready --> BookingReady : profile complete
    }

    ProfileIncomplete --> BookingReady : complete profile

    BookingReady --> BookingCreated : confirm booking
    BookingCreated --> AwaitingApproval : pending admin
    AwaitingApproval --> Approved : admin approves
    AwaitingApproval --> Rejected : admin rejects

    Approved --> CarInUse
    CarInUse --> Returned : car returned
    Returned --> CarAvailable

    Rejected --> RefundPending : if Khalti payment
    RefundPending --> RefundCompleted : refund processed
```

## 7. Activity Diagram
```mermaid
flowchart TD
    Start([Start])
    Login[Customer login / register]
    Browse[Browse cars and offers]
    Check[Check profile, CanBook, car availability]
    ProfileComplete{Is profile complete?}
    CompleteProfile[Complete profile in RentController]
    ChooseCar[Choose car / rental date]
    ChoosePayment[Choose payment method]
    PaymentMethod{Payment method}
    Cash[Cash booking]
    Khalti[Khalti payment]
    SaveRequest["Save Rent(Verified=false) and mark Car.IsCarBooked=true"]
    Pending[Pending admin approval]
    AdminReview[Admin reviews request in RentsController]
    Approve{Approve request?}
    Approved[Set Rent.Verified=true and record RentalHistory]
    End([End])

    Start --> Login --> Browse --> Check --> ProfileComplete
    ProfileComplete -- yes --> ChooseCar
    ProfileComplete -- no --> CompleteProfile --> ChooseCar
    ChooseCar --> ChoosePayment --> PaymentMethod
    PaymentMethod -- Cash --> Cash --> SaveRequest
    PaymentMethod -- Khalti --> Khalti --> SaveRequest
    SaveRequest --> Pending --> AdminReview --> Approve
    Approve -- yes --> Approved --> End
    Approve -- no --> End
```

## 8. Component Diagram
```mermaid
flowchart LR
    Browser[Web Browser / Customer UI]

    subgraph WebApp[ASP.NET Core MVC Application]
        AccountController
        HomeController
        RentController
        RentsController
        OfferController
        FestivalService
        RecommendationService
        EmailService
        DbContext[HajurKoCarRentalContext]
        Identity[ASP.NET Identity]
        Views[Razor Views]
        Models[Domain Models]
    end

    DB[(MySQL Database)]
    SMTP[(SMTP Email Server)]
    Khalti[(Khalti Payment Gateway)]

    Browser -->|HTTP/HTTPS| WebApp
    WebApp --> DbContext
    WebApp --> Identity
    WebApp --> SMTP
    WebApp --> Khalti
    DbContext --> DB
    Views --> Browser
    Models --> DbContext
```

## 9. Deployment Diagram
```mermaid
flowchart LR
    Browser[Customer Browser]
    WebServer[Web Server\nASP.NET Core MVC App]
    DB[(MySQL Database)]
    SMTP[(SMTP Email Server)]
    Khalti[(Khalti Payment Gateway)]
    FileStore[Static Files\nwwwroot/images]

    Browser -->|HTTPS| WebServer
    WebServer -->|ORM / SQL| DB
    WebServer -->|SMTP| SMTP
    WebServer -->|HTTPS API| Khalti
    WebServer --> FileStore
```

## 10. Refinement of Diagrams for Analysis
These diagrams are the refined analysis models for the system. They use the actual entities, lifecycle states, controller interactions, and workflow decisions from the HajurKoCarRental application.

### 10.1 Refined Class Diagram
```mermaid
classDiagram
    direction TB

    class Customer {
        +string Id
        +string Firstname
        +string Lastname
        +string Email
        +bool IsProfileComplete
        +bool CanBook
    }

    class Admin {
        +string Id
        +string Email
        +void ApproveRent()
    }

    class Car {
        +string Id
        +string ModelName
        +int Price
        +bool IsCarBooked
        +bool Available
    }

    class Rent {
        +string Id
        +DateTime RentalDate
        +decimal Amount
        +bool Verified
        +string PaymentMethod
    }

    class Payment {
        +string Id
        +int Amount
        +string TransactionId
        +string PaymentType
    }

    class Offer {
        +string Id
        +string OfferTitle
        +int DiscountPercentage
        +bool IsBooked
    }

    class Festival {
        +int Id
        +string Name
        +decimal PriceMultiplier
        +bool IsActive
    }

    class RentalHistory {
        +string Id
        +bool Status
        +DateTime Timestamp
    }

    class HomeController {
        +IActionResult BeforeKhalti(int carId)
    }

    class RentController {
        +IActionResult Index()
        +IActionResult RentDescription(int carId)
        +Task<IActionResult> SubmitRequest(...)
    }

    class RentsController {
        +IActionResult Index()
        +Task<IActionResult> Edit(Guid id)
    }

    Customer "1" -- "0..*" Rent : requests
    Car "1" -- "0..*" Rent : booked in
    Rent "1" -- "1" Payment : uses
    Rent "1" -- "0..1" Offer : may include
    Rent "1" -- "0..1" Festival : may apply
    Rent "1" -- "0..*" RentalHistory : records
    Admin "1" -- "0..*" Rent : approves
```

### 10.2 Refined Object Diagram
```mermaid
classDiagram
    direction LR

    class customer1~Customer~ {
        Id = "user-001"
        Firstname = "Sita"
        Email = "sita@example.com"
        IsProfileComplete = true
        CanBook = true
    }

    class car1~Car~ {
        Id = "car-101"
        ModelName = "Toyota Corolla"
        Price = 3500
        IsCarBooked = true
        Available = false
    }

    class rent1~Rent~ {
        Id = "rent-202"
        RentalDate = "2026-06-10"
        Amount = 4200
        Verified = false
        PaymentMethod = "Khalti"
    }

    class payment1~Payment~ {
        Id = "pay-303"
        Amount = 4200
        TransactionId = "KHL-789"
        PaymentType = "Khalti"
    }

    class festival1~Festival~ {
        Id = 1
        Name = "Dashain Special"
        PriceMultiplier = 1.2
        IsActive = true
    }

    customer1 -- rent1 : requested
    car1 -- rent1 : booked in
    rent1 -- payment1 : paid by
    rent1 ..> festival1 : uses
```

### 10.3 Refined State Diagram
```mermaid
stateDiagram-v2
    [*] --> LoggedOut
    LoggedOut --> LoggedIn : User logs in
    LoggedIn --> CheckBooking : selects car

    state CheckBooking {
        [*] --> Ready
        Ready --> ProfileIncomplete : profile incomplete
        Ready --> BookingReady : profile complete
        Ready --> CannotBook : CanBook == false
    }

    ProfileIncomplete --> BookingReady : complete profile
    BookingReady --> PaymentChoice : choose payment method
    PaymentChoice --> CashSelected : cash selected
    PaymentChoice --> KhaltiSelected : Khalti selected
    CashSelected --> BookingCreated : submit cash booking
    KhaltiSelected --> PaymentSuccess : Khalti callback success
    PaymentSuccess --> BookingCreated
    BookingCreated --> AwaitingApproval : rent stored, Verified=false
    AwaitingApproval --> Approved : admin approves
    AwaitingApproval --> Rejected : admin rejects
    Approved --> CarInUse : rental begins
    CarInUse --> Returned : customer returns car
    Returned --> CarAvailable : car freed
    Rejected --> RefundPending : if payment was online
    RefundPending --> RefundCompleted : refund issued
```

### 10.4 Refined Sequence Diagram
```mermaid
sequenceDiagram
    actor Customer
    actor Admin
    participant HomeController
    participant RentController
    participant RentsController
    participant Database
    participant Khalti

    Customer->>HomeController: request BeforeKhalti(carId)
    HomeController->>Database: load user and car
    HomeController->>HomeController: validate profile and availability
    alt profile incomplete
        HomeController->>RentController: redirect to Rent.Index
        RentController->>Database: load user profile
        Customer->>RentController: submit profile update
        RentController->>Database: save profile
        RentController-->>Customer: redirect to RentDescription
    end

    Customer->>RentController: open RentDescription(carId)
    RentController->>Database: load car and user
    RentController-->>Customer: show booking form

    Customer->>RentController: confirm Khalti payment
    RentController->>Khalti: initiate payment session
    Khalti-->>RentController: callback success
    RentController->>Database: save Rent(Verified=false)
    RentController->>Database: set Car.IsCarBooked=true
    RentController-->>Customer: booking submitted

    Admin->>RentsController: view pending rents
    Admin->>RentsController: approve rent
    RentsController->>Database: update Rent.Verified=true
    RentsController->>Database: add RentalHistory
    RentsController-->>Customer: send approval notification
```

### 10.5 Refined Activity Diagram
```mermaid
flowchart TD
    Start([Start])
    Login[Login / register]
    Browse[Browse car detail]
    CheckProfile[Check profile and CanBook]
    ProfileStatus{Profile complete?}
    CompleteProfile[Complete profile in RentController]
    ChoosePayment[Choose payment method]
    PaymentMethod{Payment method}
    Khalti[Khalti payment]
    Cash[Cash booking]
    SaveRequest["Save Rent(Verified=false) and mark Car.IsCarBooked=true"]
    Pending[Pending admin approval]
    AdminReview[Admin reviews request]
    Approve{Approve request?}
    Approved[Set Verified=true]
    Rejected[Reject / request correction]
    End([End])

    Start --> Login --> Browse --> CheckProfile --> ProfileStatus
    ProfileStatus -- yes --> ChoosePayment
    ProfileStatus -- no --> CompleteProfile --> ChoosePayment
    ChoosePayment --> PaymentMethod
    PaymentMethod -- Khalti --> Khalti --> SaveRequest
    PaymentMethod -- Cash --> Cash --> SaveRequest
    SaveRequest --> Pending --> AdminReview --> Approve
    Approve -- yes --> Approved --> End
    Approve -- no --> Rejected --> End
```

